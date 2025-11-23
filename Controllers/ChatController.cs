using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kallemny.Data;
using Kallemny.DTOs;
using Kallemny.Models;

namespace Kallemny.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("rooms")]
        public async Task<ActionResult<ChatRoomResponse>> CreateChatRoom(CreateChatRoomRequest request)
        {
            var userId = GetUserId();

            if (!request.IsGroup && request.OtherUserId.HasValue)
            {
                var existingRoom = await _context.ChatRooms
                    .Include(cr => cr.ChatRoomUsers)
                    .Where(cr => !cr.IsGroup && cr.ChatRoomUsers.Count == 2)
                    .FirstOrDefaultAsync(cr => 
                        cr.ChatRoomUsers.Any(cu => cu.UserId == userId) &&
                        cr.ChatRoomUsers.Any(cu => cu.UserId == request.OtherUserId.Value));

                if (existingRoom != null)
                {
                    return Ok(new ChatRoomResponse
                    {
                        Id = existingRoom.Id,
                        Name = existingRoom.Name,
                        IsGroup = existingRoom.IsGroup,
                        CreatedAt = existingRoom.CreatedAt
                    });
                }
            }

            var chatRoom = new ChatRoom
            {
                Name = request.Name,
                IsGroup = request.IsGroup,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            _context.ChatRoomUsers.Add(new ChatRoomUser
            {
                UserId = userId,
                ChatRoomId = chatRoom.Id,
                JoinedAt = DateTime.UtcNow
            });

            if (!request.IsGroup && request.OtherUserId.HasValue)
            {
                _context.ChatRoomUsers.Add(new ChatRoomUser
                {
                    UserId = request.OtherUserId.Value,
                    ChatRoomId = chatRoom.Id,
                    JoinedAt = DateTime.UtcNow
                });
            }

            if (request.IsGroup && request.UserIds != null)
            {
                foreach (var uid in request.UserIds)
                {
                    if (uid != userId)
                    {
                        _context.ChatRoomUsers.Add(new ChatRoomUser
                        {
                            UserId = uid,
                            ChatRoomId = chatRoom.Id,
                            JoinedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ChatRoomResponse
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                IsGroup = chatRoom.IsGroup,
                CreatedAt = chatRoom.CreatedAt
            });
        }

        [HttpGet("rooms")]
        public async Task<ActionResult<List<ChatRoomResponse>>> GetChatRooms()
        {
            var userId = GetUserId();

            var chatRooms = await _context.ChatRoomUsers
                .Where(cu => cu.UserId == userId)
                .Include(cu => cu.ChatRoom)
                .ThenInclude(cr => cr.ChatRoomUsers)
                .ThenInclude(cu => cu.User)
                .Select(cu => new ChatRoomResponse
                {
                    Id = cu.ChatRoom.Id,
                    Name = cu.ChatRoom.Name,
                    IsGroup = cu.ChatRoom.IsGroup,
                    CreatedAt = cu.ChatRoom.CreatedAt,
                    UserNames = cu.ChatRoom.ChatRoomUsers
                        .Select(cru => cru.User.Username)
                        .ToArray()
                })
                .ToListAsync();

            return Ok(chatRooms);
        }

        [HttpGet("rooms/{roomId}/messages")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessages(
            int roomId, 
            [FromQuery] int skip = 0, 
            [FromQuery] int take = 50)
        {
            var userId = GetUserId();

            var isMember = await _context.ChatRoomUsers
                .AnyAsync(cu => cu.UserId == userId && cu.ChatRoomId == roomId);

            if (!isMember)
            {
                return Forbid();
            }

            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == roomId)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .Include(m => m.Sender)
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderUsername = m.Sender.Username,
                    SenderId = m.SenderId,
                    SentAt = m.SentAt,
                    ChatRoomId = m.ChatRoomId
                })
                .ToListAsync();

            messages.Reverse();

            return Ok(messages);
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserPresence>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserPresence
                {
                    UserId = u.Id,
                    Username = u.Username,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .ToListAsync();

            return Ok(users);
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }
    }
}
