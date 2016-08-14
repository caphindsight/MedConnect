using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Helpers;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using MedConnectMongoLib;

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
            BotConfig.Data.Messages.ForwardPattern.Replace("{name}", name).Replace("{text}", text);

        private static string NewRecipientMessageText(string name) =>
            BotConfig.Data.Messages.NewRecipientPattern.Replace("{name}", name);

        private static string UnknownCommandMessageText(string command) =>
            BotConfig.Data.Messages.UnknownCommandPattern.Replace("{command}", command);

        private async Task ForwardToAdmin(long recipientId, Message msg) {
            if (BotConfig.Data.Telegram.ForwardToAdmin && recipientId != BotConfig.Data.Telegram.AdminId) {
                await Bot.ForwardMessageAsync(BotConfig.Data.Telegram.AdminId, msg.Chat.Id, msg.MessageId);
            }
        }

        private async Task ReportErrorToAdmin(Exception e) {
            if (BotConfig.Data.Telegram.ReportErrorsToAdmin) {
                await Bot.SendTextMessageAsync(BotConfig.Data.Telegram.AdminId, $"Exception occured:\n{e}");
            }
        }

        private async Task ReplyText(long recipientId, string text) {
            Message msg = await Bot.SendTextMessageAsync(recipientId, text);
            await ForwardToAdmin(recipientId, msg);
        }

        private async Task Broadcast(Room room, string text = null) {
            RoomMember[] us = (from member in room.Members where member.TelegramId == Data.Id select member).ToArray();
            RoomMember[] others = (from member in room.Members where member.TelegramId != Data.Id select member).ToArray();

            if (us.Length > 1)
                throw new MongoException($"Duplicate room member {Data.Id} in room {room.RoomId}");

            if (us.Length == 0)
                throw new MongoException($"This should not happen, ever!");

            RoomMember me = us[0];

            foreach (RoomMember member in others) {
                string forwardedText = text ?? ForwardMessageText(me.Name, Data.Text);
                await ReplyText(member.TelegramId, forwardedText);
            }
        }

        public async Task Process() {
            try {
                await ProcessNormal();
            } catch (Exception e) {
                await ReportErrorToAdmin(e);
            }
        }

        public async Task ProcessNormal() {
            await ForwardToAdmin(Data.Id, Data.Message);

            Room[] rooms = await Mongo.FindRooms(Data.Id);
            Room currentRoom = GlobalCache.CurrentRoomCache.Get(Data.Id);

            RoomMember me = null;
            if (currentRoom != null) {
                foreach (RoomMember member in currentRoom.Members) {
                    if (member.TelegramId == Data.Id)
                        me = member;
                }
            }

            if (rooms.Length == 0) {
                await ReplyText(Data.Id, BotConfig.Data.Messages.NoRoomsMessage);
            } else if (Data.Text == "/start" || Data.Text == "/help") {
                await ReplyText(Data.Id, BotConfig.Data.Messages.HelpMessage);
            } else if (Data.Text == "/select") {
                await ReplyKeyboard(rooms);
            } else if (Data.Text == "/over") {
                if (currentRoom == null) {
                    await ReplyKeyboard(rooms);
                    return;
                }

                if (me.Role != MemberRole.Doctor) {
                    await ReplyText(Data.Id, BotConfig.Data.Messages.OnlyDoctorsCanCloseDialogsMessage);
                    return;
                }

                try {
                    await Broadcast(currentRoom, BotConfig.Data.Messages.DoctorHasClosedTheDialogMessage);
                } catch {}

                await Mongo.DeleteRoom(currentRoom.RoomId);

                await ReplyText(Data.Id, BotConfig.Data.Messages.DialogRemovedMessage);
            } else if (Data.Text.StartsWith("/")) {
                await ReplyText(Data.Id, UnknownCommandMessageText(Data.Text));
            } else {
                foreach (Room room in rooms) {
                    string localTitle = room.GetLocalTitle(Data.Id);
                    if (localTitle == Data.Text) {
                        GlobalCache.CurrentRoomCache.Set(Data.Id, room);
                        await ReplyText(Data.Id, NewRecipientMessageText(localTitle));
                        return;
                    }
                }

                if (rooms.Length == 1) {
                    GlobalCache.CurrentRoomCache.Set(Data.Id, rooms[0]);
                    currentRoom = rooms[0];
                }

                if (currentRoom != null) {
                    await Broadcast(currentRoom);
                } else {
                    await ReplyKeyboard(rooms);
                }
            }
        }

        private async Task ReplyKeyboard(Room[] rooms) {
            KeyboardButton[] buttons = (from room in rooms select new KeyboardButton(room.GetLocalTitle(Data.Id))).ToArray();
            await Bot.SendTextMessageAsync(Data.Id, BotConfig.Data.Messages.ChooseYourRecipientMessage,
                replyMarkup: new ReplyKeyboardMarkup(VerticalKeyboard(buttons), true, true));
        }

        private static KeyboardButton[][] VerticalKeyboard(KeyboardButton[] buttons) {
            int n = buttons.Length;
            var res = new KeyboardButton[n][];
            for (int i = 0; i < n; i++) {
                res[i] = new KeyboardButton[] { buttons[i] };
            }
            return res;
        }
    }

    public sealed class BotContextData {
        public Message Message { get; set; }
        public long Id { get; set; }
        public string Text { get; set; }
    }
}
