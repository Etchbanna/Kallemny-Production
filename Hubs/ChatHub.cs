using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Kallemny.Data;
using Kallemny.Models;
using Kallemny.DTOs;

namespace Kallemny.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private static readonly Dictionary<string, HashSet<string>> _userConnections = new();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var username = GetUsername();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            lock (_userConnections)
            {
                if (!_userConnections.ContainsKey(userId.ToString()))
                {
                    _userConnections[userId.ToString()] = new HashSet<string>();
                }
                _userConnections[userId.ToString()].Add(Context.ConnectionId);
            }

            var chatRoomIds = await _context.ChatRoomUsers
                .Where(cu => cu.UserId == userId)
                .Select(cu => cu.ChatRoomId)
                .ToListAsync();

            foreach (var roomId in chatRoomIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{roomId}");
            }

            await Clients.All.SendAsync("UserOnline", new UserPresence
            {
                UserId = userId,
                Username = username,
                IsOnline = true,
                LastSeen = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var username = GetUsername();

            bool isLastConnection = false;
            lock (_userConnections)
            {
                if (_userConnections.ContainsKey(userId.ToString()))
                {
                    _userConnections[userId.ToString()].Remove(Context.ConnectionId);
                    if (_userConnections[userId.ToString()].Count == 0)
                    {
                        _userConnections.Remove(userId.ToString());
                        isLastConnection = true;
                    }
                }
            }

            if (isLastConnection)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await Clients.All.SendAsync("UserOffline", new UserPresence
                {
                    UserId = userId,
                    Username = username,
                    IsOnline = false,
                    LastSeen = DateTime.UtcNow
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            var userId = GetUserId();
            var username = GetUsername();

            var isMember = await _context.ChatRoomUsers
                .AnyAsync(cu => cu.UserId == userId && cu.ChatRoomId == request.ChatRoomId);

            if (!isMember)
            {
                throw new HubException("You are not a member of this chat room");
            }

            var message = new Message
            {
                Content = request.Content,
                SenderId = userId,
                ChatRoomId = request.ChatRoomId,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var messageResponse = new MessageResponse
            {
                Id = message.Id,
                Content = message.Content,
                SenderUsername = username,
                SenderId = userId,
                SentAt = message.SentAt,
                ChatRoomId = message.ChatRoomId
            };

            await Clients.Group($"ChatRoom_{request.ChatRoomId}")
                .SendAsync("ReceiveMessage", messageResponse);
        }

        public async Task JoinChatRoom(int chatRoomId)
        {
            var userId = GetUserId();

            var isMember = await _context.ChatRoomUsers
                .AnyAsync(cu => cu.UserId == userId && cu.ChatRoomId == chatRoomId);

            if (!isMember)
            {
                throw new HubException("You are not a member of this chat room");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
        }

        public async Task LeaveChatRoom(int chatRoomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ChatRoom_{chatRoomId}");
        }

        public async Task SendTypingIndicator(int chatRoomId, bool isTyping)
        {
            var userId = GetUserId();
            var username = GetUsername();

            var isMember = await _context.ChatRoomUsers
                .AnyAsync(cu => cu.UserId == userId && cu.ChatRoomId == chatRoomId);

            if (!isMember)
            {
                return;
            }

            var typingIndicator = new TypingIndicator
            {
                UserId = userId,
                Username = username,
                ChatRoomId = chatRoomId,
                IsTyping = isTyping
            };

            await Clients.OthersInGroup($"ChatRoom_{chatRoomId}")
                .SendAsync("UserTyping", typingIndicator);
        }

        public async Task MarkMessagesAsRead(int chatRoomId)
        {
            var userId = GetUserId();

            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId 
                    && m.SenderId != userId 
                    && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            await Clients.OthersInGroup($"ChatRoom_{chatRoomId}")
                .SendAsync("MessagesRead", new { chatRoomId, userId });
        }

        private int GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        private string GetUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }
    }
}
