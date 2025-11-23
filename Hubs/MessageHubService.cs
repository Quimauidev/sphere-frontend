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

namespace Sphere.Hubs
{
    public class MessageHubService
    {
        private readonly HubConnection _connection;

        // Tất cả conversation đã join trên server
        private readonly HashSet<Guid> _joinedConversations = new();

        // Conversation thực sự đang mở trên UI
        private readonly HashSet<Guid> _activeConversations = new();

        // Chỉ đăng ký event 1 lần
        private bool _eventsRegistered = false;

        // Event cho frontend subscribe
        public event Action<MessageModel>? OnMessageReceived;
        public event Action<Guid, string>? OnError;
        public event Action<Guid, string>? OnMessagesMarkedAsRead;
        public event Action<Guid>? OnMessageSentConfirmed;
        public event Action<Guid>? OnMessageDelivered;
        public event Action<Guid>? OnMessageSeen;

        public MessageHubService(HubConfig config)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(config.HubUrl, options =>
                {
                    options.AccessTokenProvider = () =>
                        Task.FromResult<string?>(PreferencesHelper.GetAuthToken() ?? string.Empty);
                })
                .WithAutomaticReconnect()
                .Build();

            RegisterEvents();

            _connection.Reconnected += async (_) =>
            {
                foreach (var convId in _activeConversations)
                {
                    try
                    {
                        await _connection.InvokeAsync("JoinConversation", convId);
                        _joinedConversations.Add(convId);
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
            if (_eventsRegistered) return;

            _connection.On<MessageModel>("ReceiveMessage", msg => OnMessageReceived?.Invoke(msg));
            _connection.On<string>("Error", msg => OnError?.Invoke(Guid.Empty, msg));
            _connection.On<Guid, string>("MessagesMarkedAsRead", (convId, userId) => OnMessagesMarkedAsRead?.Invoke(convId, userId));
            _connection.On<Guid>("MessageSent", id => OnMessageSentConfirmed?.Invoke(id));
            _connection.On<Guid>("MessageDelivered", id => OnMessageDelivered?.Invoke(id));
            _connection.On<Guid>("MessageSeen", id => OnMessageSeen?.Invoke(id));

            _eventsRegistered = true;
        }

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

        /// <summary>Tham gia conversation để nhận tin nhắn</summary>
        public async Task JoinConversation(Guid conversationId)
        {
            if (!_activeConversations.Contains(conversationId))
                _activeConversations.Add(conversationId);

            if (_connection.State != HubConnectionState.Connected)
                await StartAsync();

            try
            {
                if (!_joinedConversations.Contains(conversationId))
                {
                    await _connection.InvokeAsync("JoinConversation", conversationId);
                    _joinedConversations.Add(conversationId);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(conversationId, $"JoinConversation failed: {ex.Message}");
            }
        }

        /// <summary>Rời conversation khi đóng hoặc đổi sang conversation khác</summary>
        public async Task LeaveConversation(Guid conversationId)
        {
            _activeConversations.Remove(conversationId);
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

        /// <summary>Gửi message với messageId tạm thời, backend trả lại cùng Id</summary>
        public async Task SendMessage(Guid conversationId, string content, Guid messageId)
        {
            if (_connection.State != HubConnectionState.Connected)
                await StartAsync();

            try
            {
                await _connection.InvokeAsync("SendMessage", conversationId, content, messageId);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(conversationId, $"SendMessage failed: {ex.Message}");
                throw; // để ViewModel bắt lỗi và đánh dấu Failed
            }
        }

        public async Task NotifySeenAsync(Guid messageId, Guid conversationId)
        {
            if (_connection.State != HubConnectionState.Connected)
                return;

            await _connection.InvokeAsync("NotifySeen", messageId, conversationId);
        }

        /// <summary>Kiểm tra conversation đang active hay không</summary>
        public bool IsActiveConversation(Guid conversationId) => _activeConversations.Contains(conversationId);
    }

}