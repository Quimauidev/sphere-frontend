using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sphere.Models.Request;

namespace Sphere.ViewModels
{
    public partial class MessageViewModel : ObservableObject
    {
        private readonly IConversationService _conversationService;
        private readonly IUserSessionService _userSessionService;
        private readonly MessageHubService _hubService;
        private readonly SQLiteMessageService _localDb;

        public ObservableCollection<MessageModel> Messages { get; set; } = new();

        [ObservableProperty]
        private string? partnerAvatar;

        [ObservableProperty]
        private string? partnerFullName;

        // thêm phương thức để gán dữ liệu
        public void SetPartner(UserDiaryModel partner)
        {
            PartnerFullName = partner.FullName;
            // Xác định avatar hiển thị
            if (!string.IsNullOrWhiteSpace(partner.AvatarUrl))
                PartnerAvatar = partner.AvatarUrl;
            else
                PartnerAvatar = partner.Gender == Gender.Female ? "woman.png" : "man.png";
        }

        [ObservableProperty]
        private Guid conversationId;

        [ObservableProperty]
        private string? currentMessage;

        private bool _isLoadingOlderMessages = false;


        public MessageViewModel(IConversationService conversationService, IUserSessionService userSessionService, MessageHubService hubService, SQLiteMessageService localDb)
        {
            _conversationService = conversationService;
            _userSessionService = userSessionService;
            _hubService = hubService;
            _localDb = localDb;
            // Nhận tin nhắn realtime
            // Nhận tin nhắn mới
            _hubService.OnMessageReceived += async msg =>
            {
                if (msg.ConversationId != ConversationId) return;

                // Ensure IsMine is set correctly for incoming message
                msg.IsMine = msg.SenderId == _userSessionService.CurrentUser?.UserDTO?.Id;

                var existing = Messages.FirstOrDefault(m => m.Id == msg.Id);
                if (existing != null)
                {
                    // Update fields and map status
                    existing.Content = msg.Content;
                    existing.SentAt = msg.SentAt;
                    existing.IsRead = msg.IsRead;
                    existing.ReceiverId = msg.ReceiverId;

                    // If message is mine, backend may return Sent/Delivered/Seen
                    if (existing.IsMine)
                    {
                        existing.Status = msg.Status;
                    }
                    else
                    {
                        // For incoming messages, mark as Delivered by default
                        existing.Status = msg.Status;
                    }
                }
                else
                {
                    msg.Status = msg.IsMine ? (msg.Status == 0 ? MessageStatus.Sent : msg.Status) : msg.Status;
                    Messages.Add(msg);
                    ScrollToLastMessage?.Invoke();
                }
                await _localDb.SaveMessagesAsync([_localDb.MapModelToEntity(msg)]);
                UpdateLastMessageFlag();
            };

            // Xử lý xác nhận gửi xong
            _hubService.OnMessageSentConfirmed += messageId =>
            {
                var tempMessage = Messages.FirstOrDefault(m => m.Id == messageId);
                if (tempMessage != null)
                {
                    tempMessage.Status = MessageStatus.Sent;
                    UpdateMessageStatusIcons();
                }
            };

            // Khi backend báo delivered
            _hubService.OnMessageDelivered += messageId =>
            {
                var m = Messages.FirstOrDefault(x => x.Id == messageId);
                if (m != null)
                {
                    m.Status = MessageStatus.Delivered;
                    UpdateMessageStatusIcons();
                }
            };

            // Khi backend báo seen
            _hubService.OnMessageSeen += messageId =>
            {
                var m = Messages.FirstOrDefault(x => x.Id == messageId);
                if (m != null)
                {
                    m.Status = MessageStatus.Seen;
                    UpdateMessageStatusIcons();
                }
            };

            _hubService.OnMessagesMarkedAsRead += (convId, userId) =>
            {
                if (convId == ConversationId)
                {
                    foreach (var msg in Messages)
                    {
                        if (!msg.IsMine)
                            msg.IsRead = true;
                    }
                }
            };
        }

        // Action để page scroll xuống cuối
        public Action? ScrollToLastMessage { get; set; }
        public Action<MessageModel>? ScrollToMessage { get; set; }


        partial void OnConversationIdChanged(Guid value)
        {
            _ = LoadMessagesAsync(value);
            _ = _hubService.JoinConversation(value);
        }

        private async Task LoadMessagesAsync(Guid conversationId)
        {
            if (conversationId == Guid.Empty) return;

            Messages.Clear();

            var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;

            // B1: Load từ SQLite (rất nhanh)
            var localMsgs = await _localDb.GetMessagesAsync(conversationId, 50);

            foreach (var e in localMsgs.OrderBy(m => m.SentAt))
            {
                Messages.Add(_localDb.MapEntityToModel(e, currentUserId));
            }
            ScrollToLastMessage?.Invoke();
            if (Messages.Count > 0)
            {
                UpdateLastMessageFlag();
                UpdateMessageStatusIcons();
                ScrollToLastMessage?.Invoke();
            }

            // B2: Load từ server (chỉ 1 lần)
            var response = await _conversationService.GetLatestMessagesAsync(conversationId);

            if (!response.IsSuccess || response.Data == null)
                return;

            // B3: Lưu vào SQLite
            var entities = response.Data.Select(m => _localDb.MapModelToEntity(m)).ToList();
            await _localDb.SaveMessagesAsync(entities);

            // B4: Update UI (đẩy tin mới hơn vào nếu có)
            Messages.Clear();
            foreach (var m in response.Data.OrderBy(m => m.SentAt))
            {
                m.IsMine = m.SenderId == currentUserId;
                Messages.Add(m);
            }

            UpdateLastMessageFlag();
            UpdateMessageStatusIcons();
            ScrollToLastMessage?.Invoke();
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (ConversationId == Guid.Empty || string.IsNullOrWhiteSpace(CurrentMessage))
                return;

            // Tạo tin nhắn tạm thời
            var tempMessage = new MessageModel
            {
                Id = Guid.NewGuid(), // tạm thời
                ConversationId = ConversationId,
                SenderId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty,
                ReceiverId = Guid.Empty,
                Content = CurrentMessage,
                SentAt = DateTime.UtcNow,
                IsMine = true,
                Status = MessageStatus.Sending
            };

            Messages.Add(tempMessage);
            UpdateLastMessageFlag();
            ScrollToLastMessage?.Invoke();

            // Xóa text editor
            MainThread.BeginInvokeOnMainThread(() => CurrentMessage = string.Empty);

            try
            {
                // Gửi tin nhắn kèm Id tạm thời để backend trả về cùng Id
                await _hubService.SendMessage(ConversationId, tempMessage.Content, tempMessage.Id);

                // Sau khi backend xác nhận, hub sẽ gửi lại, tempMessage.Status được cập nhật trong OnMessageReceived
            }
            catch
            {
                tempMessage.Status = MessageStatus.Sending; // giữ trạng thái gửi lỗi
            }
        }

        private void UpdateMessageStatusIcons()
        {
            var myMessages = Messages.Where(m => m.IsMine).ToList();

            if (!myMessages.Any()) return;

            // tìm tin cuối chưa seen
            var lastNotSeen = myMessages
                .Where(m => m.Status != MessageStatus.Seen)
                .LastOrDefault();

            foreach (var m in myMessages)
            {
                // CHỈ cập nhật khi giá trị thực sự đổi → tránh OnPropertyChanged dư
                bool newShow =
                    m.Status == MessageStatus.Sending ||
                    m.Status == MessageStatus.Seen ||
                    m == lastNotSeen;

                if (m.ShowStatusIcon != newShow)
                    m.ShowStatusIcon = newShow;
            }
        }

        private void UpdateLastMessageFlag()
        {
            if (Messages.Count == 0) return;

            // Tin nhắn cũ hết là false
            if (Messages.Count > 1)
                Messages[Messages.Count - 2].IsLastMessage = false;

            // Tin nhắn cuối cùng là true
            Messages[Messages.Count - 1].IsLastMessage = true;
        }

        [RelayCommand]
        private async Task LoadOlderMessagesAsync()
        {
            if (_isLoadingOlderMessages || !Messages.Any()) return;
            _isLoadingOlderMessages = true;

            var firstMessage = Messages.First();
            var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;

            // Lấy Id của item đầu tiên để scroll lại sau
            var firstVisibleMessageId = firstMessage.Id;

            // 1️⃣ Load server
            var response = await _conversationService.GetMessagesBeforeAsync(ConversationId, firstMessage.Id, 50);
            if (response.IsSuccess && response.Data != null && response.Data.Any())
            {
                var entities = response.Data.Select(m => _localDb.MapModelToEntity(m)).ToList();
                await _localDb.SaveMessagesAsync(entities);

                // Insert lên đầu
                foreach (var m in response.Data.OrderBy(m => m.SentAt))
                {
                    m.IsMine = m.SenderId == currentUserId;
                    Messages.Insert(0, m);
                }

                // Delay nhỏ để CollectionView cập nhật
                await Task.Delay(50);

                // Scroll lại về item đầu tiên cũ
                var item = Messages.FirstOrDefault(m => m.Id == firstVisibleMessageId);
                if (item != null)
                {
                    ScrollToMessage?.Invoke(item);
                }
            }

            _isLoadingOlderMessages = false;
        }


        [RelayCommand]
        private void OpenGallery() => App.Current!.MainPage!.DisplayAlert("Gallery", "Open gallery", "OK");

        [RelayCommand]
        private void SendLocation() => App.Current!.MainPage!.DisplayAlert("Location", "Send location", "OK");

        [RelayCommand]
        private void Attach() => App.Current!.MainPage!.DisplayAlert("Attach", "Open attachments", "OK");
    }
}