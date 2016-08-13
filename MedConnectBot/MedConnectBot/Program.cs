using System;
using System.Threading;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Helpers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using MedConnectBot.Mongo;
using MedConnectBot.Tele;

namespace MedConnectBot {
    public static class Program {
        private static readonly MongoCtl Mongo_;
        private static readonly TelegramBotClient Bot_;

        static Program() {
            Mongo_ = new MongoCtl(BotConfig.Data.Mongo.ConnectionString, BotConfig.Data.Mongo.Database);
            Bot_ = new TelegramBotClient(BotConfig.Data.Telegram.AccessToken);
        }

        private static async void OnMessage(object sender, MessageEventArgs args) {
            long id = args.Message.Chat.Id;
            string text = args.Message.Text;
            var data = new BotContextData() {
                Id = id,
                Text = text,
            };

            var ctx = new BotContext(Bot_, Mongo_, data);
            await ctx.Process();
        }

        public static void Main(string[] args) {
            Console.WriteLine("Checking mongo connection..");
            if (!Mongo_.CheckSalt(BotConfig.Data.Mongo.Salt).Result) {
                throw new Exception("Invalid salt in mongo configuration");
            }

            Bot_.OnMessage += OnMessage;

            Bot_.StartReceiving();

            Telegram.Bot.Types.User me = Bot_.GetMeAsync().Result;
            Console.WriteLine($"Telegram bot {me.Username} is active!");
            Console.WriteLine("Press ESC to quit..");

            for (;;) {
                while (Console.KeyAvailable) {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                        goto abort;
                }

                Thread.Sleep(100);
            }

            abort:
            Bot_.StopReceiving();
            Console.WriteLine("Bye.");
        }
    }
}
