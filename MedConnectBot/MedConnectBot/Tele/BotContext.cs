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

        private async Task ForwardToAdmin(long recipientId, Message msg) {
            if (BotConfig.Data.Telegram.ForwardToAdmin && recipientId != BotConfig.Data.Telegram.AdminId) {
                await Bot.ForwardMessageAsync(BotConfig.Data.Telegram.AdminId, msg.Chat.Id, msg.MessageId);
            }
        }

        private async Task SendText(long recipientId, string text) {
            Message msg = await Bot.SendTextMessageAsync(recipientId, text);
            await ForwardToAdmin(recipientId, msg);
        }

        private async Task Broadcast(Room room, MedConnectBot.Mongo.User user) {
            foreach (RoomMember member in room.Members) {
                if (member.TelegramId != Data.Id) {
                    string forwardedText = ForwardMessageText(user.Name, Data.Text);
                    await SendText(member.TelegramId, forwardedText);
                }
            }
        }

        public async Task Process() {
            await ForwardToAdmin(Data.Id, Data.Message);

            MedConnectBot.Mongo.User user = await GlobalCache.UserCache.GetOrUpdate(Data.Id, (long id) => {
                Console.WriteLine($"User cache query failed for {id}");
                return Mongo.GetUser(id);
            });

            Room[] rooms = await GlobalCache.RoomCache.GetOrUpdate(Data.Id, (long id) => {
                Console.WriteLine($"Rooms cache query failed for {id}");
                return Mongo.FindRooms(id);
            });

            Room currentRoom = GlobalCache.CurrentRoomCache.Get(Data.Id);

            if (user == null) {
                await SendText(Data.Id, "Sorry, but there is no record of you in our database");
            } else if (rooms.Length == 0) {
                await SendText(Data.Id, "You are not mentioned in any of the chat rooms");
            } else if (rooms.Length == 1) {
                Room room = rooms[0];
                GlobalCache.CurrentRoomCache.Set(Data.Id, room);
                await Broadcast(room, user);
            } else {
                if (currentRoom != null) {
                    await Broadcast(currentRoom, user);
                } else {
                    await SendText(Data.Id, "You are mentioned in multiple chat rooms");
                }
            }
        }
    }

    public sealed class BotContextData {
        public Message Message { get; set; }
        public long Id { get; set; }
        public string Text { get; set; }
    }
}
