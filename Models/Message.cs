using System;
using System.ComponentModel.DataAnnotations;

namespace Kallemny.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        [Required]
        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        [Required]
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;
    }
}
