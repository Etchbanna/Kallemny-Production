using System;

namespace Kallemny.Models
{
    public class ChatRoomUser
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
