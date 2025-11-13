using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using static Android.Provider.ContactsContract;
using static Sphere.Models.Request;

namespace Sphere.ViewModels
{
    public partial class MessageViewModel : ObservableObject
    {
        private readonly IConversationService _conversationService;
        private readonly IUserSessionService _userSessionService;
        private readonly MessageHubService _hubService;

        public ObservableCollection<MessageModel> Messages { get; set; } = new();

        [ObservableProperty]
        private string? partnerAvatar;

        [ObservableProperty]
        private string? partnerFullName;

        // thêm phương thức để gán dữ liệu
        public void SetPartner(UserDiaryModel partner)
        {
            PartnerFullName = partner.FullName;
            PartnerAvatar = partner.AvatarUrl;
        }

        [ObservableProperty]
        private Guid conversationId;

        [ObservableProperty]
        private string? currentMessage;

        public MessageViewModel(
            IConversationService conversationService,
            IUserSessionService userSessionService,
            MessageHubService hubService)
        {
            _conversationService = conversationService;
            _userSessionService = userSessionService;
            _hubService = hubService;

            // Nhận tin nhắn realtime
            _hubService.OnMessageReceived += msg =>
            {
                if (msg.ConversationId == ConversationId)
                {
                    msg.IsMine = msg.SenderId == _userSessionService.CurrentUser?.UserDTO?.Id;
                    Messages.Add(msg);

                    // Scroll xuống cuối
                    ScrollToLastMessage?.Invoke();
                }
            };

            _hubService.OnMessagesMarkedAsRead += (convId, userId) =>
            {
                if (convId == ConversationId)
                {
                    foreach (var msg in Messages)
                    {
                        if (!msg.IsMine) msg.IsRead = true;
                    }
                }
            };
        }

        // Action để page scroll xuống cuối
        public Action? ScrollToLastMessage { get; set; }

        partial void OnConversationIdChanged(Guid value)
        {
            _ = LoadMessagesAsync(value);
            _ = _hubService.JoinConversation(value);
        }

        private async Task LoadMessagesAsync(Guid conversationId)
        {
            if (conversationId == Guid.Empty) return;

            var response = await _conversationService.GetMessagesAsync(conversationId);
            if (response.IsSuccess && response.Data != null)
            {
                Messages.Clear();
                var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;
                foreach (var m in response.Data.OrderBy(m => m.SentAt))
                {
                    m.IsMine = m.SenderId == currentUserId;
                    Messages.Add(m);
                }

                // Scroll xuống cuối sau khi load
                ScrollToLastMessage?.Invoke();
            }
            else
            {
                await ApiResponseHelper.ShowApiErrorsAsync(response, "Không thể tải tin nhắn");
            }
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (ConversationId == Guid.Empty || string.IsNullOrWhiteSpace(CurrentMessage))
                return;

            try
            {
                // Gửi tin nhắn qua Hub
                await _hubService.SendMessage(ConversationId, CurrentMessage);

                // Clear Editor trên main thread, tránh lỗi Android
                MainThread.BeginInvokeOnMainThread(() => CurrentMessage = string.Empty);

                // Scroll xuống cuối
                ScrollToLastMessage?.Invoke();
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert("Lỗi gửi tin nhắn", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void OpenGallery() => App.Current!.MainPage!.DisplayAlert("Gallery", "Open gallery", "OK");

        [RelayCommand]
        private void SendLocation() => App.Current!.MainPage!.DisplayAlert("Location", "Send location", "OK");

        [RelayCommand]
        private void Attach() => App.Current!.MainPage!.DisplayAlert("Attach", "Open attachments", "OK");
    }
}
