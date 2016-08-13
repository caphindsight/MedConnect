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

            Users_ = Database_.GetCollection<BsonDocument>("users");
            Rooms_ = Database_.GetCollection<BsonDocument>("rooms");
        }

        private readonly IMongoClient Client_;
        private readonly IMongoDatabase Database_;

        private readonly BsonDocument EmptyFilter_ = new BsonDocument();

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

        public Task<Room[]> FindRooms(string telegramId) {
            return Collect<Room>(Rooms_, EmptyFilter_, (BsonDocument doc) => {
                bool admit = false;

                string roomId = doc.GetValue("r_id").AsString;
                var members = new List<RoomMember>();

                BsonArray bsonMembers = doc.GetValue("members").AsBsonArray;
                foreach (BsonValue bsonMember in bsonMembers) {
                    string tid = bsonMember.AsBsonDocument.GetValue("t_id").AsString;
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
}
