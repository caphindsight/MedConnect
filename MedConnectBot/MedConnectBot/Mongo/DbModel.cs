using System;
using System.Linq;

namespace MedConnectBot.Mongo {
    public sealed class Room {
        public string RoomId { get; set; }
        public string LocalTitle { get; set; }
        public RoomMember[] Members { get; set; }

        public string GetLocalTitle(long me) {
            return String.Join(", ", from member in Members where member.TelegramId != me select member.Name);
        }
    }

    public sealed class RoomMember {
        public long TelegramId { get; set; }
        public string Name { get; set; }
        public MemberRole Role { get; set; }
    }

    public enum MemberRole {
        Client,
        Doctor,
    }
}
