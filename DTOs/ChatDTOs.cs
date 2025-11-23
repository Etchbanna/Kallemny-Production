using System;
using System.ComponentModel.DataAnnotations;

namespace Kallemny.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class SendMessageRequest
    {
        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int ChatRoomId { get; set; }
    }

    public class MessageResponse
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public DateTime SentAt { get; set; }
        public int ChatRoomId { get; set; }
    }

    public class CreateChatRoomRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsGroup { get; set; } = false;

        public int? OtherUserId { get; set; }

        public int[]? UserIds { get; set; }
    }

    public class ChatRoomResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; }
        public string[]? UserNames { get; set; }
    }

    public class UserPresence
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class TypingIndicator
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public bool IsTyping { get; set; }
    }
}
