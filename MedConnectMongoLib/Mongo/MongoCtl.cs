﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MedConnectMongoLib {
    public sealed class MongoCtl {
        public MongoCtl(string connectionString, string database) {
            Client_ = new MongoClient(connectionString);
            Database_ = Client_.GetDatabase(database);

            Config_ = Database_.GetCollection<BsonDocument>("config");
            Rooms_ = Database_.GetCollection<BsonDocument>("rooms");
            Doctors_ = Database_.GetCollection<BsonDocument>("doctors");
            MagicHashes_ = Database_.GetCollection<BsonDocument>("magic_hashes");
        }

        private readonly IMongoClient Client_;
        private readonly IMongoDatabase Database_;

        private readonly BsonDocument EmptyFilter_ = new BsonDocument();

        private readonly IMongoCollection<BsonDocument> Config_;
        private readonly IMongoCollection<BsonDocument> Rooms_;
        private readonly IMongoCollection<BsonDocument> Doctors_;
        private readonly IMongoCollection<BsonDocument> MagicHashes_;

        private async Task Process(IMongoCollection<BsonDocument> collection, BsonDocument filter, Action<BsonDocument> action) {
            using (var cursor = await collection.FindAsync(filter)) {
                while (await cursor.MoveNextAsync()) {
                    var batch = cursor.Current;
                    foreach (BsonDocument document in batch) {
                        action(document);
                    }
                }
            }
        }

        private async Task<T[]> Collect<T>(IMongoCollection<BsonDocument> collection, BsonDocument filter, Func<BsonDocument, T> conv)
            where T: class
        {
            var res = new List<T>();
            await Process(collection, filter, (BsonDocument doc) => {
                T t = conv(doc);
                if (t != null) {
                    res.Add(t);
                }
            });
            return res.ToArray();
        }

        public async Task<MagicHash> GenerateAndStoreMagicHashes(DoctorInfo doctor) {
            string magicHash = Guid.NewGuid().ToString().Substring(0, 6);
            var document = new BsonDocument {
                { "magic_hash", magicHash },
                {"doctor", new BsonDocument {
                        { "telegram_id", doctor.TelegramId.ToString() },
                        { "displayed_name", "nastya" }
                     }
                }
            };
            await MagicHashes_.InsertOneAsync(document);
            return new MagicHash {
                Value = magicHash,
            };
        }

        public async Task<string> GetSalt() {
            string salt = null;
            await Process(Config_, EmptyFilter_, (BsonDocument doc) => {
                string docSalt = doc.GetValue("salt").AsString;
                salt = docSalt;
            });
            return salt;
        }

        public Task<Room[]> FindRooms(long telegramId) {
            return Collect<Room>(Rooms_, EmptyFilter_, (BsonDocument doc) => {
                bool admit = false;

                string roomId = doc.GetValue("r_id").AsString;
                var members = new List<RoomMember>();

                BsonArray bsonMembers = doc.GetValue("members").AsBsonArray;
                foreach (BsonValue bsonMember in bsonMembers) {
                    long tid = Convert.ToInt64(bsonMember.AsBsonDocument.GetValue("t_id").AsString);
                    string name = bsonMember.AsBsonDocument.GetValue("name").AsString;
                    string roleStr = bsonMember.AsBsonDocument.GetValue("role").AsString;

                    MemberRole role;
                    switch (roleStr) {
                    case "client":
                        role = MemberRole.Client;
                        break;

                    case "doctor":
                        role = MemberRole.Doctor;
                        break;

                    default:
                        throw new MongoException($"Unknown member role: '{roleStr}'");
                    }

                    members.Add(new RoomMember() {
                        TelegramId = tid,
                        Name = name,
                        Role = role,
                    });

                    if (tid == telegramId) {
                        admit = true;
                    }
                }

                if (admit) {
                    return new Room() {
                        RoomId = roomId,
                        Members = members.ToArray(),
                    };
                } else {
                    return null;
                }
            });
        }

        public async Task DeleteRoom(string roomId) {
            var filter = new BsonDocument();
            filter.Set("r_id", roomId);
            await Rooms_.DeleteOneAsync(filter);
        }

        public async Task<DoctorInfo> FindSingleDoctor(long telegramId) {
            DoctorInfo[] doctors = await Collect<DoctorInfo>(Doctors_, EmptyFilter_, (BsonDocument doc) => new DoctorInfo () {
                TelegramId = Convert.ToInt64(doc.GetValue("t_id").AsString),
            });
            if (doctors.Length > 1) {
                throw new Exception("too much doctors");
            } else if (doctors.Length == 0) {
                return null;
            } else {
                return doctors[0];
            }
        }

        public Task<DoctorInfo[]> FindDoctors() {
            return Collect<DoctorInfo>(Doctors_, EmptyFilter_, (BsonDocument doc) => new DoctorInfo() {
                TelegramId = Convert.ToInt64(doc.GetValue("t_id").AsString),
                Name = doc.GetValue("name").AsString,
                Speciality = doc.GetValue("speciality").AsString,
                Education = doc.GetValue("education").AsString,

                Courses = (
                    from bsonCourseVal in doc.GetValue("raise_qualification_courses").AsBsonArray
                    let bsonCourseDoc = bsonCourseVal.AsBsonDocument
                    select new RaiseQualificationCourse() {
                        Name = bsonCourseDoc.GetValue("name").AsString,
                        Year = bsonCourseDoc.GetValue("year").AsString,
                        Place = bsonCourseDoc.GetValue("place").AsString,
                    }
                ).ToArray(),

                Certificates = (
                    from bsonCertVal in doc.GetValue("medical_certificates").AsBsonArray
                    select new MedicalCertificate() {
                        Name = bsonCertVal.AsBsonDocument.GetValue("name").AsString,
                    }
                ).ToArray(),

                Miscellaneous = doc.GetValue("miscellaneous").AsString,
            });
        }
    }

    public sealed class MongoException : Exception {
        public MongoException(string msg)
            : base(msg)
        {}
    }
}