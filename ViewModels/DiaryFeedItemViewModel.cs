using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Hubs;
using Sphere.Models;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Views.Controls;
using Sphere.Views.Pages;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Android.Provider.Telephony.Sms;
using static Sphere.Models.Request;

namespace Sphere.ViewModels
{
    public partial class DiaryFeedItemViewModel : ObservableObject
    {
        // Services
        private readonly IFollowService _followService;
        private readonly IConversationService _conversationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldShowFollowButton))] // cập nhật UI khi thay đổi trạng thái
        private bool isFollowing;

        [ObservableProperty] private bool isLoading;

        [ObservableProperty]
        private Guid userId;

        public DiaryFeedItemViewModel(UserWithDiaryModel model, Guid currentUserId, IFollowService followService, IConversationService conversationService, IServiceProvider serviceProvider)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _followService = followService;
            _conversationService = conversationService;
            _serviceProvider = serviceProvider;
            var user = model.UserDiaryDTO ?? throw new ArgumentNullException(nameof(model.UserDiaryDTO));
            UserId = user.Id;

            // 🔹 Thiết lập trạng thái online ban đầu
            IsOnline = PresenceService.OnlineUsersCache.TryGetValue(UserId, out var online)
                ? online
                : user.IsOnline;
           
            // ✅ Đăng ký lắng nghe sự kiện online/offline realtime
            WeakReferenceMessenger.Default.Register<UserStatusChangedMessage>(this, (r, m) =>
            {
                if (m.UserId == UserId)
                {
                    IsOnline = m.IsOnline; // realtime update
                }
            });

            IsOwnFollow = user.Id == currentUserId;
            IsOwnMess = user.Id == currentUserId;

            WeakReferenceMessenger.Default.Register<UserFollowChangedMessage>(this, (r, m) =>
            {
                if (Model.UserDiaryDTO?.Id == m.UserId)
                {
                    IsFollowing = m.IsFollowing;
                }
            });

            // Chỉ khởi tạo trạng thái follow nếu không phải post của chính mình
            if (!IsOwnFollow)
            {
                IsFollowing = user.IsFollow;
            }
        }

        [ObservableProperty]
        private bool isOnline;

        public bool IsOwnFollow { get; }
        public bool IsOwnMess { get; }
        public UserWithDiaryModel Model { get; }
        public bool ShouldShowChatButton => !IsOwnMess; // ẩn nếu là bài của chính mình
        public bool ShouldShowFollowButton => !IsOwnFollow && !IsFollowing;

        // Thêm property lưu id user cần follow
        [RelayCommand]
        public async Task ChatAsync()
        {
            if (IsOwnMess) return;
            bool alreadyUnlocked = PreferencesHelper.IsChatUnlocked(Model.UserDiaryDTO!.Id);
            if (!alreadyUnlocked)
            {
                bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Xác nhận mở khóa",
                "Cần tiêu 130 kim cương 💎 để mở khóa cuộc trò chuyện này. Bạn có muốn tiếp tục không?",
                "Đồng ý", "Hủy");

                if (!confirm)
                    return;
            }    

            var response = await _conversationService.StartConversationAsync(Model.UserDiaryDTO!.Id);
            if (response.Errors?.Any(e => e.Code == "NotEnoughDiamonds") == true ||
                response.Message?.Contains("kim cương", StringComparison.OrdinalIgnoreCase) == true)
            {
                bool goTopUp = await Application.Current!.MainPage!.DisplayAlert(
                 "Không đủ kim cương 💎",
                 "Bạn không đủ kim cương để mở khóa cuộc trò chuyện này. Bạn có muốn nạp thêm không?",
                 "Nạp ngay", "Đóng");

                if (goTopUp)
                {
                    var topUpPage = _serviceProvider.GetRequiredService<HomePage>();
                    await Application.Current!.MainPage!.Navigation.PushModalAsync(topUpPage);
                }
                return;
            }
            if (response.IsSuccess && response.Data?.ConversationId is Guid conId)
            {
                PreferencesHelper.SetChatUnlocked(Model.UserDiaryDTO.Id, true);
                if (!alreadyUnlocked)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Mở khóa thành công",
                   $"Bạn đã mở khóa cuộc trò chuyện. Số dư còn lại: {response.Data.NewBalance} 💎", "OK");
                }
                

                var mess = _serviceProvider.GetRequiredService<MessagePage>();
                if (mess.BindingContext is MessageViewModel vm)
                {
                    vm.ConversationId = response.Data.ConversationId.Value;
                    // Gọi trực tiếp từ UserDiaryDTO
                    vm.SetPartner(Model.UserDiaryDTO!);
                }
                await Application.Current!.MainPage!.Navigation.PushModalAsync(mess);

            }
            else
                await ApiResponseHelper.ShowApiErrorsAsync(response, "Không thể mở chat");
        }

        

        [RelayCommand]
        public async Task Follow()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var response = await _followService.FollowUserAsync(UserId);
                if (response.IsSuccess)
                {
                    IsFollowing = true;
                    // Gửi message thông báo follow đã thay đổi
                    WeakReferenceMessenger.Default.Send(new UserFollowChangedMessage(UserId, true));
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response, "Theo dõi thất bại");
                }
            }
            finally
            {
                PopupHelper.HideLoading();
                IsLoading = false;
            }
        }
    }

   
}