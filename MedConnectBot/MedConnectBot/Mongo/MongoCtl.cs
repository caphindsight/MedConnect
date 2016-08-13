using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MedConnectBot.Mongo {
    public sealed class MongoCtl {
        public MongoCtl(string connectionString, string database) {
            Client_ = new MongoClient(connectionString);
            Database_ = Client_.GetDatabase(database);

            Config_ = Database_.GetCollection<BsonDocument>("config");
            Users_ = Database_.GetCollection<BsonDocument>("users");
            Rooms_ = Database_.GetCollection<BsonDocument>("rooms");
        }

        private readonly IMongoClient Client_;
        private readonly IMongoDatabase Database_;

        private readonly BsonDocument EmptyFilter_ = new BsonDocument();

        private readonly IMongoCollection<BsonDocument> Config_;
        private readonly IMongoCollection<BsonDocument> Users_;
        private readonly IMongoCollection<BsonDocument> Rooms_;

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

        public async Task<bool> CheckSalt(string salt) {
            bool result = false;
            await Process(Config_, EmptyFilter_, (BsonDocument doc) => {
                string docSalt = doc.GetValue("salt").AsString;
                if (docSalt == salt) {
                    result = true;
                }
            });
            return result;
        }

        public async Task<User> GetUser(long telegramId) {
            var filter = new BsonDocument();
            filter.Set("t_id", telegramId.ToString());

            User[] users = await Collect<User>(Users_, filter, (BsonDocument doc) => {
                long tid = Convert.ToInt64(doc.GetValue("t_id").AsString);
                if (tid != telegramId) {
                    throw new MongoException("Telegram id filter is not working somehow");
                }

                string name = doc.GetValue("name").AsString;
                string roleStr = doc.GetValue("role").AsString;
                UserRole role;

                switch (roleStr) {
                case "client":
                    role = UserRole.Client;
                    break;

                case "doctor":
                    role = UserRole.Doctor;
                    break;

                default:
                    throw new MongoException($"Unknown user role: {roleStr}");
                }

                return new User() {
                    TelegramId = tid,
                    Name = name,
                    Role = role,
                };
            });

            if (users.Length == 0) {
                return null;
            } else if (users.Length == 1) {
                return users[0];
            } else {
                throw new MongoException($"Found duplicate user with telegram id {telegramId}");
            }
        }

        public Task<Room[]> FindRooms(long telegramId) {
            return Collect<Room>(Rooms_, EmptyFilter_, (BsonDocument doc) => {
                bool admit = false;

                string roomId = doc.GetValue("r_id").AsString;
                var members = new List<RoomMember>();

                BsonArray bsonMembers = doc.GetValue("members").AsBsonArray;
                foreach (BsonValue bsonMember in bsonMembers) {
                    long tid = Convert.ToInt64(bsonMember.AsBsonDocument.GetValue("t_id").AsString);
                    members.Add(new RoomMember() {
                        TelegramId = tid,
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
    }

    public sealed class MongoException : Exception {
        public MongoException(string msg)
            : base(msg)
        {}
    }
}
