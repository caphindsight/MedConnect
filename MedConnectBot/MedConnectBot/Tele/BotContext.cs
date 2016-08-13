using System;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Helpers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using MedConnectBot.Mongo;

namespace MedConnectBot.Tele {
    public sealed class BotContext {
        public TelegramBotClient Bot { get; private set; }
        public MongoCtl Mongo { get; private set; }
        public BotContextData Data { get; private set; }

        public BotContext(TelegramBotClient bot, MongoCtl mongo, BotContextData data) {
            Bot = bot;
            Mongo = mongo;
            Data = data;
        }

        private static string ForwardMessageText(string name, string text) =>
            BotConfig.Data.Telegram.ForwardPattern.Replace("{name}", name).Replace("{text}", text);

        private async Task Broadcast(Room room, MedConnectBot.Mongo.User user) {
            foreach (RoomMember member in room.Members) {
                if (member.TelegramId != Data.Id) {
                    string forwardedText = ForwardMessageText(user.Name, Data.Text);
                    await Bot.SendTextMessageAsync(member.TelegramId, forwardedText);
                }
            }
        }

        public async Task Process() {
            MedConnectBot.Mongo.User user = await GlobalCache.UserCache.GetOrUpdate(Data.Id, (long id) => {
                Console.WriteLine($"User cache query failed for {id}");
                return Mongo.GetUser(id);
            });

            Room[] rooms = await GlobalCache.RoomCache.GetOrUpdate(Data.Id, (long id) => {
                Console.WriteLine($"Rooms cache query failed for {id}");
                return Mongo.FindRooms(id);
            });

            if (user == null) {
                await Bot.SendTextMessageAsync(Data.Id, "Sorry, but there is no record of you in our database");
            } else if (rooms.Length == 0) {
                await Bot.SendTextMessageAsync(Data.Id, "You are not mentioned in any of the chat rooms");
            } else if (rooms.Length == 1) {
                Room room = rooms[0];
                await Broadcast(room, user);
            } else {
                await Bot.SendTextMessageAsync(Data.Id, "You are mentioned in multiple chat rooms");
            }
        }
    }

    public sealed class BotContextData {
        public long Id { get; set; }
        public string Text { get; set; }
    }
}
