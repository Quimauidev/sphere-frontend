using Microsoft.AspNetCore.SignalR.Client;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    public class MessageHubService
    {
        private readonly HubConnection _connection;
        private readonly HashSet<Guid> _joinedConversations = new();

        // Sự kiện frontend sẽ subscribe
        public event Action<MessageModel>? OnMessageReceived;

        public event Action<Guid, string>? OnError;

        public event Action<Guid, string>? OnMessagesMarkedAsRead;

        // Event riêng để update trạng thái message đã gửi xong
        public event Action<Guid>? OnMessageSentConfirmed;

        // Event khi message được delivered/seen
        public event Action<Guid>? OnMessageDelivered;
        public event Action<Guid>? OnMessageSeen;

        public MessageHubService(HubConfig config)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(config.HubUrl, options =>
                {
                    options.AccessTokenProvider = () =>
                        Task.FromResult(PreferencesHelper.GetAuthToken() ?? string.Empty);
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterEvents();

            // Khi reconnect, join lại các conversation đã tham gia
            _connection.Reconnected += async (_) =>
            {
                foreach (var convId in _joinedConversations)
                {
                    try
                    {
                        await _connection.InvokeAsync("JoinConversation", convId);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(convId, $"Reconnect failed: {ex.Message}");
                    }
                }
            };
        }

        private void RegisterEvents()
        {
            // Nhận tin nhắn từ backend (broadcast từ service)
            _connection.On<MessageModel>("ReceiveMessage", msg =>
            {
                OnMessageReceived?.Invoke(msg);
            });

            // Khi backend báo lỗi
            _connection.On<string>("Error", msg =>
            {
                OnError?.Invoke(Guid.Empty, msg);
            });

            // Khi đánh dấu đã đọc
            _connection.On<Guid, string>("MessagesMarkedAsRead", (conversationId, userId) =>
            {
                OnMessagesMarkedAsRead?.Invoke(conversationId, userId);
            });

            // Optional: xác nhận gửi tin nhắn
            _connection.On<Guid>("MessageSent", messageId =>
            {
                OnMessageSentConfirmed?.Invoke(messageId);
            });

            // Khi backend báo tin nhắn đã được delivered
            _connection.On<Guid>("MessageDelivered", messageId =>
            {
                OnMessageDelivered?.Invoke(messageId);
            });

            // Khi backend báo tin nhắn đã được seen
            _connection.On<Guid>("MessageSeen", messageId =>
            {
                OnMessageSeen?.Invoke(messageId);
            });
        }

        // Kết nối hub
        public async Task StartAsync()
        {
            if (_connection.State == HubConnectionState.Connected)
                return;

            try
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(Guid.Empty, $"Cannot connect to hub: {ex.Message}");
            }
        }

        // Tham gia conversation để nhận tin nhắn
        public async Task JoinConversation(Guid conversationId)
        {
            if (!_joinedConversations.Contains(conversationId))
                _joinedConversations.Add(conversationId);

            if (_connection.State != HubConnectionState.Connected)
                await StartAsync();

            try
            {
                await _connection.InvokeAsync("JoinConversation", conversationId);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(conversationId, $"JoinConversation failed: {ex.Message}");
            }
        }

        // Rời khỏi conversation
        public async Task LeaveConversation(Guid conversationId)
        {
            if (_joinedConversations.Contains(conversationId))
                _joinedConversations.Remove(conversationId);

            if (_connection.State != HubConnectionState.Connected) return;

            try
            {
                await _connection.InvokeAsync("LeaveConversation", conversationId);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(conversationId, $"LeaveConversation failed: {ex.Message}");
            }
        }

        // Gửi tin nhắn
        public async Task SendMessage(Guid conversationId, string content, Guid messageId)
        {
            if (_connection.State != HubConnectionState.Connected)
                await StartAsync();

            try
            {
                // Gửi messageId kèm content để backend trả lại cùng Id
                await _connection.InvokeAsync("SendMessage", conversationId, content, messageId);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(conversationId, $"SendMessage failed: {ex.Message}");
            }
        }
    }
}