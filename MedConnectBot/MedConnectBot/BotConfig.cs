using System;
using System.IO;
using System.Web.Script.Serialization;

namespace MedConnectBot {
    public sealed class BotConfig {
        public MongoSettings Mongo { get; private set; }

        private static readonly JavaScriptSerializer Serializer_ =
            new JavaScriptSerializer();

        public static readonly BotConfig Data;

        private const string BotConfigFile_ = "bot_config.json";

        static BotConfig() {
            var configFile = new FileInfo(BotConfigFile_);

            if (!configFile.Exists) {
                throw new ConfigException($"Config file not found: '{BotConfigFile_}'");
            }

            string configStr = File.ReadAllText(configFile.FullName);

            Data = Serializer_.Deserialize<BotConfig>(configStr);
        }
    }

    public sealed class MongoSettings {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }

        public bool RequiresAuthorization { get; set; }

        public string User { get; set; }
        public string Password { get; set; }

        public string ConnectionString {
            get {
                string auth = RequiresAuthorization ? $"{User}:{Password}@" : String.Empty;
                return $"mongodb://{auth}{Host}:{Port}/{Database}";
            }
        }
    }

    public sealed class ConfigException : Exception {
        public ConfigException(string message)
            : base(message)
        {}
    }
}
