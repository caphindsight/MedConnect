using System;

namespace MedConnectBot.Mongo {
    public sealed class Room {
        public string RoomId { get; set; }
        public RoomMember[] Members { get; set; }
    }

    public sealed class RoomMember {
        public string TelegramId { get; set; }
    }
}
