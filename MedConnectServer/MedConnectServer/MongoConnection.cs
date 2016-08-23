using System;

using MedConnectMongoLib;

namespace MedConnectServer {
    public static class MongoConnection {
        private static readonly MongoCtl Mongo_;

        static MongoConnection() {
            Mongo_ = new MongoCtl(ServerConfig.Data.Mongo.ConnectionString, ServerConfig.Data.Mongo.Database);
        }

        public static MongoCtl MongoCtl {
            get { return Mongo_; }
        }
    }
}
