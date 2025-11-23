using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Kallemny.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsGroup { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        public ICollection<ChatRoomUser> ChatRoomUsers { get; set; } = new List<ChatRoomUser>();
    }
}
