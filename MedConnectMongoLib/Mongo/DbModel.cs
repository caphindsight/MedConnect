using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MedConnectMongoLib {
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

    public sealed class DoctorInfo {
        public long TelegramId { get; set; }
        public string Name { get; set; }
        public string Speciality { get; set; }
        public string Education { get; set; }
        public RaiseQualificationCourse[] Courses { get; set; }
        public MedicalCertificate[] Certificates { get; set; }
        public string Miscellaneous { get; set; }
    }

    public sealed class RaiseQualificationCourse {
        public string Name { get; set; }
        public string Year { get; set; }
        public string Place { get; set; }
    }

    public sealed class MedicalCertificate {
        public string Name { get; set; }
    }

    public sealed class MagicHash {
        public string Value { get; set; }
        private static readonly Regex GuidRegex_ = new Regex(@"^[a-zA-Z0-9]{6}$");
        public static bool IsMagicHashCandidate(string expression) {
            return expression != null && GuidRegex_.IsMatch(expression);
        }
    }
}
