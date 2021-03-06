﻿using System;
using System.IO;
using System.Web.Script.Serialization;

namespace MedConnectBot {
    public sealed class BotConfig {
        public MongoSettings Mongo { get; private set; }
        public TelegramSettings Telegram { get; private set; }
        public CachingSettings Caching { get; private set; }
        public MessagesSettings Messages { get; private set; }

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

    public sealed class TelegramSettings {
        public string AccessToken { get; private set; }
        public long AdminId { get; private set; }
        public bool ReportErrorsToAdmin { get; private set; }
        public bool ForwardToAdmin { get; private set; }
    }

    public sealed class CachingSettings {
        public double CurrentRoomCacheInvalidationTimeMinutes { get; private set; }
    }

    public sealed class MessagesSettings {
        public string ForwardPattern { get; private set; }
        public string NewRecipientPattern { get; private set; }
        public string UnknownCommandPattern { get; private set; }
        public string NoRoomsPattern { get; private set; }
        public string ChooseYourRecipientMessage { get; private set; }
        public string OnlyDoctorsCanCloseDialogsMessage { get; private set; }
        public string DoctorHasClosedTheDialogMessage { get; private set; }
        public string DialogRemovedMessage { get; private set; }
        public string HelpMessage { get; private set; }
    }

    public sealed class ConfigException : Exception {
        public ConfigException(string message)
            : base(message)
        {}
    }
}
