using System;

namespace MedConnectBot.Mongo {
    public sealed class Room {
        public string RoomId { get; set; }
        public RoomMember[] Members { get; set; }
    }

    public sealed class RoomMember {
        public long TelegramId { get; set; }
    }

    public sealed class User {
        public long TelegramId { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }
    }

    public enum UserRole {
        Client,
        Doctor,
    }
}
