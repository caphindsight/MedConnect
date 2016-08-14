using System;
using System.IO;
using System.Web.Script.Serialization;

namespace MedConnectServer {
    public sealed class ServerConfig {
        public MongoSettings Mongo { get; private set; }
        public HttpSettings Http { get; private set; }

        private static readonly JavaScriptSerializer Serializer_ =
            new JavaScriptSerializer();

        public static readonly ServerConfig Data;

        private const string ServerConfigFile_ = "server_config.json";

        static ServerConfig() {
            var configFile = new FileInfo(ServerConfigFile_);

            if (!configFile.Exists) {
                throw new ConfigException($"Config file not found: '{ServerConfigFile_}'");
            }

            string configStr = File.ReadAllText(configFile.FullName);

            Data = Serializer_.Deserialize<ServerConfig>(configStr);
        }
    }

    public sealed class MongoSettings {
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Database { get; private set; }

        public bool RequiresAuthorization { get; private set; }

        public string User { get; private set; }
        public string Password { get; private set; }

        public string Salt { get; private set; }

        public string ConnectionString {
            get {
                string auth = RequiresAuthorization ? $"{User}:{Password}@" : String.Empty;
                return $"mongodb://{auth}{Host}:{Port}/{Database}";
            }
        }
    }

    public sealed class HttpSettings {
        public string AddrMask { get; private set; }
        public string Port { get; private set; }

        public string Url {
            get {
                return $"http://{AddrMask}:{Port}";
            }
        }
    }

    public sealed class ConfigException : Exception {
        public ConfigException(string message)
            : base(message)
        {}
    }
}
