using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Extensions;
using Sphere.Interfaces;
using Sphere.Models;
using Sphere.Models.Params;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.ViewModels.DiaryViewModels;
using Sphere.Views.Controls;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Android.Graphics.ColorSpace;

namespace Sphere.ViewModels
{
    public partial class ProfileViewModel : ObservableObject, IModalParameterReceiver<Guid?>
    {
        private readonly IImagePickerService _imagePickerService;
        private readonly IUserProfileService _userProfileService;
        private readonly IUserSessionService _userSession;
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
        private readonly ApiResponseHelper _res;
        private readonly IFollowService _followService;
        private readonly IConversationService _conversationService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldShowFollowButton))]
        [NotifyPropertyChangedFor(nameof(ShouldShowChatButton))]
        private bool isFollowing;
        public bool IsOwnFollow { get; }
        [ObservableProperty]
        public partial bool ShouldShowBioToggle { get; set; }
        public DiaryListViewModel DiaryListVM { get; }
        public ProfileViewModel(IUserSessionService userSession, IImagePickerService imagePickerService, IUserProfileService userProfileService, IDiaryService diaryService,IAppNavigationService anv, IShellNavigationService nv, ApiResponseHelper res, IFollowService followService, IConversationService conversationService)
        {
            _userSession = userSession;
            _imagePickerService = imagePickerService;
            _userProfileService = userProfileService;
            _anv = anv;
            _nv = nv;
            _res = res;
            _followService = followService;
            _conversationService = conversationService;
            var restoredUser = PreferencesHelper.LoadCurrentUser();
            if (restoredUser != null)
            {
                _userSession.CurrentUser = restoredUser;
            }
            CurrentUser = _userSession.CurrentUser;
            DiaryListVM = new DiaryListViewModel(diaryService, _anv, _nv,_res);
        }
        public bool ShouldShowFollowButton => !IsViewingSelf && !IsFollowing;
        public bool ShouldShowChatButton => !IsViewingSelf;
        [ObservableProperty]
        private Guid? viewingUserId;
        //public bool IsViewingSelf => !ViewingUserId.HasValue;
        public bool IsViewingSelf => !ViewingUserId.HasValue || ViewingUserId == _userSession.CurrentUser?.UserDTO?.Id;
        partial void OnViewingUserIdChanged(Guid? value)
        {
            OnPropertyChanged(nameof(IsViewingSelf)); // 🔥 bắt buộc
            OnPropertyChanged(nameof(ShouldShowFollowButton));
            OnPropertyChanged(nameof(ShouldShowChatButton));
        }
        public async Task Receive(Guid? userId)
        {
            ViewingUserId = userId;

            if (userId == null)
            {
                // 👉 profile của mình
                CurrentUser = _userSession.CurrentUser;
                await DiaryListVM.LoadFirstPage();
            }
            else
            {
                // 👉 profile người khác
                await LoadOtherUser(userId.Value);
            }
        }
        private async Task LoadOtherUser(Guid userId)
        {
            IsLoading = true;
            try
            {
                var resp = await _userProfileService.GetUserProfileOtherAsync(userId);

                if (!resp.IsSuccess)
                {
                    await _anv.DisplayAlertAsync("Lỗi", $"{resp.Message}");
                    return;
                }

                CurrentUser = new UserWithUserProfileModel
                {
                    UserDTO = resp.Data!.UserDTO,
                    UserProfileDTO = resp.Data.UserProfileDTO,
                    
                };
                IsFollowing = resp.Data.IsFollowing;
                await DiaryListVM.LoadFirstPage(userId);
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }
        [RelayCommand]
        public async Task Follow()
        {
            if (IsFollowing || ViewingUserId == null) return;

            var res = await _followService.FollowUserAsync(ViewingUserId.Value);

            if (res.IsSuccess)
            {
                IsFollowing = true;
            }
            else
            {
                await _res.ShowApiErrorsAsync(res, "Theo dõi thất bại");
            }
        }
        public string? AvatarDisplay => string.IsNullOrWhiteSpace(CurrentUser?.UserProfileDTO?.AvatarUrl) ? (CurrentUser?.UserDTO?.Gender == Gender.Female ? "woman.png" : "man.png") : CurrentUser.UserProfileDTO.AvatarUrl;

        public string BioDisplay => string.IsNullOrWhiteSpace(CurrentUser?.UserProfileDTO?.Bio) ? "Xin chào! Tôi là người bí ẩn mới tham gia" : CurrentUser.UserProfileDTO.Bio;

        // Giá trị MaxLines thay đổi theo trạng thái mở rộng
        public int BioMaxLines => IsBioExpanded ? int.MaxValue : 3;

        [ObservableProperty]
        public partial BioProfileModel? BioProfileModel { get; set; }

        // Nhấn nút chuyển trạng thái
        public string BioToggleText => IsBioExpanded ? "Thu gọn" : "Xem thêm";

        public string BirthDayDisplay => CurrentUser?.UserDTO?.BirthDay.HasValue == true ? CurrentUser.UserDTO.BirthDay.Value.ToString("dd/MM/yyyy") : string.Empty;

        public long Coins => CurrentUser?.UserProfileDTO?.Coins ?? 0;

        public string CoverPhotoDisplay => string.IsNullOrWhiteSpace(CurrentUser?.UserProfileDTO?.CoverPhotoUrl)
                ? "anhbia.jpg"
                : CurrentUser.UserProfileDTO.CoverPhotoUrl;

        public string CreatedAtDisplay => CurrentUser!.UserDTO!.CreatedAt!.ToVietnamTimeString();

        [ObservableProperty]
        public partial UserWithUserProfileModel? CurrentUser { get; set; }

        public string FullNameDisplay => CurrentUser?.UserDTO?.FullName ?? string.Empty;

        public long UserIdNumberDisplay => CurrentUser?.UserDTO?.UserIdNumber ?? 0;
        public string GenderDisplay => CurrentUser?.UserDTO?.Gender switch
        {
            Gender.Male => "Nam",
            Gender.Female => "Nữ",
            null => string.Empty,
            _ => throw new NotImplementedException(),
        };
        //public string GenderDisplay => CurrentUser?.UserDTO?.Gender == Gender.Male ? "Nam" : "Nữ";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BioMaxLines))]
        [NotifyPropertyChangedFor(nameof(BioToggleText))]
        public partial bool IsBioExpanded { get; set; }
        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [RelayCommand]
        public async Task EditBio()
        {
            if (!IsViewingSelf) return; // 🔥 CHẶN
            var oldBio = CurrentUser?.UserProfileDTO?.Bio ?? string.Empty;
            var popup = new BioEditPopup(oldBio);

            if (await Shell.Current.ShowPopupAsync(popup) is string result)
            {
                var newBio = result.Trim();
                if (newBio == oldBio.Trim())
                {
                    await ApiResponseHelper.ShowShellAlertAsync("Thông báo", "Bạn chưa thay đổi nội dung tiểu sử");
                    return;
                }
                // Tạo model phù hợp với API
                BioProfileModel ??= new BioProfileModel();
                BioProfileModel.Bio = result;
                await SaveBio();
            }
        }

        [RelayCommand]
        public async Task EditProfile()
        {
            if (!IsViewingSelf) return;

            await _nv.PushModalAsync<EditUserProfilePage>();
        }

        public void RefreshFromSession()
        {
            CurrentUser = _userSession.CurrentUser;
        }

        [RelayCommand]
        public async Task SaveBio()
        {
            if (CurrentUser == null || CurrentUser.UserProfileDTO == null)
                return;

            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

            try
            {
                Guid id = CurrentUser!.UserDTO!.Id;
                var response = await _userProfileService.UpdateBioAsync(id, BioProfileModel!);
                if (response?.Data != null)
                {
                    // Cập nhật dữ liệu local
                    CurrentUser.UserProfileDTO?.Bio = response.Data.Bio;
                    PreferencesHelper.SaveCurrentUser(CurrentUser);
                    OnPropertyChanged(nameof(BioDisplay));
                    CheckBioLength();
                }
                else
                {
                    await _res.ShowApiErrorsAsync(response!, "Thất bại");
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ShowImageOptions(string imageType)
        {
            string displayName = imageType == "avatar" ? "ảnh đại diện" : "ảnh bìa";
            string? imageUrl = imageType == "avatar" ? AvatarDisplay : CoverPhotoDisplay;
            string action;
            if (IsViewingSelf)
            {
                // 👉 của mình
                action = await Shell.Current.DisplayActionSheetAsync(
                    $"Tùy chọn {displayName}",
                    "Hủy",
                    null,
                    "Xem ảnh",
                    "Chỉnh sửa ảnh",
                    "Xóa ảnh");
            }
            else
            {
                // 👉 người khác
                action = await Shell.Current.DisplayActionSheetAsync(
                    $"Tùy chọn {displayName}",
                    "Hủy",
                    null,
                    "Xem ảnh",
                    "Tố cáo ảnh");
            }

            switch (action)
            {
                case "Xem ảnh":
                    await Shell.Current.Navigation.PushModalAsync(new ImageViewerPage(imageUrl!));
                    break;

                case "Chỉnh sửa ảnh":
                    if (IsViewingSelf)
                        await EditImageAsync(imageType);
                    break;

                case "Xóa ảnh":
                    if (IsViewingSelf)
                        await DeleteImageAsync(imageType);
                    break;

                case "Tố cáo ảnh":
                    await ReportImageAsync(imageType, imageUrl!);
                    break;
            }
        }

        [RelayCommand]
        public Task ToggleBio()
        {
            IsBioExpanded = !IsBioExpanded;
            return Task.CompletedTask;
        }
        private void CheckBioLength()
        {
            // Ví dụ: Giả sử 50 ký tự ~ 1 dòng (bạn có thể tinh chỉnh)
            int approxCharsPerLine = 28;
            int bioLength = CurrentUser?.UserProfileDTO?.Bio?.Length ?? 0;
            int lineCount = (int)Math.Ceiling((double)bioLength / approxCharsPerLine);
            ShouldShowBioToggle = lineCount > 5;
        }
        private async Task DeleteImageAsync(string imageType)
        {
            bool confirm = await ApiResponseHelper.ShowShellConfirmAsync( "Xác nhận", "Bạn có chắc muốn xoá ảnh không?", "Xoá", "Huỷ"); 
            if (!confirm)
                return;

            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

            try
            {
                Guid id = CurrentUser!.UserDTO!.Id;
                ApiResponse<UserProfileModel> response;

                if (imageType == "avatar")
                    response = await _userProfileService.DeleteAvatarAsync(id);
                else
                    response = await _userProfileService.DeleteCoverPhotoAsync(id);

                if (response.IsSuccess)
                {
                    if (imageType == "avatar")
                    {
                        CurrentUser.UserProfileDTO?.AvatarUrl = response.Data!.AvatarUrl;
                        OnPropertyChanged(nameof(AvatarDisplay));
                    }
                    else
                    {
                        CurrentUser.UserProfileDTO?.CoverPhotoUrl = response.Data!.CoverPhotoUrl;
                        OnPropertyChanged(nameof(CoverPhotoDisplay));
                    }

                    PreferencesHelper.SaveCurrentUser(CurrentUser);
                }
                else
                    await _res.ShowApiErrorsAsync(response!, "Không thể xoá ảnh");
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }

        private async Task EditImageAsync(string imageType)
        {
            var result = await _imagePickerService.PickSingleImageAsync();

            if (string.IsNullOrEmpty(result)) return;

            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

            try
            {
                Guid id = CurrentUser!.UserDTO!.Id;
                ApiResponse<UserProfileModel> response;
                if (imageType == "avatar")
                    response = await _userProfileService.UpdateAvatarAsync(id, result);
                else
                    response = await _userProfileService.UpdateCoverPhotoAsync(id, result);

                if (response.IsSuccess)
                {
                    if (imageType == "avatar")
                    {
                        CurrentUser.UserProfileDTO?.AvatarUrl = response.Data!.AvatarUrl;
                        OnPropertyChanged(nameof(AvatarDisplay));
                    }
                    else
                    {
                        CurrentUser.UserProfileDTO?.CoverPhotoUrl = response.Data!.CoverPhotoUrl;
                        OnPropertyChanged(nameof(CoverPhotoDisplay));
                    }

                    PreferencesHelper.SaveCurrentUser(CurrentUser);
                }
                else
                    await _res.ShowApiErrorsAsync(response!, "Thất bại");
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }
        private async Task ReportImageAsync(string imageType, string imageUrl)
        {
            var confirm = await Shell.Current.DisplayAlertAsync( "Tố cáo", "Bạn có chắc muốn tố cáo ảnh này?", "Có", "Hủy");

            if (!confirm) return;

            // 👉 gọi API nếu có
            // await _reportService.ReportImageAsync(...);

            await Shell.Current.DisplayAlertAsync("Thông báo", "Đã gửi tố cáo", "OK");
        }

        [RelayCommand]
        public async Task PostStatus()
        {
            if (!IsViewingSelf) return; // 🔥 CHẶN
            await _nv.PushModalAsync<PostDiaryPage>();
        }

        public bool CanEditProfile => CurrentUser?.UserDTO?.Id == _userSession.CurrentUser?.UserDTO?.Id;
        partial void OnCurrentUserChanged(UserWithUserProfileModel? oldValue, UserWithUserProfileModel? newValue)
        {
            OnPropertyChanged(nameof(FullNameDisplay));
            OnPropertyChanged(nameof(UserIdNumberDisplay));
            OnPropertyChanged(nameof(GenderDisplay));
            OnPropertyChanged(nameof(BirthDayDisplay));
            OnPropertyChanged(nameof(CreatedAtDisplay));
            OnPropertyChanged(nameof(AvatarDisplay));
            OnPropertyChanged(nameof(CoverPhotoDisplay));
            OnPropertyChanged(nameof(BioDisplay));
            OnPropertyChanged(nameof(Coins));
            CheckBioLength();
            OnPropertyChanged(nameof(CanEditProfile));
        }

        [RelayCommand]
        // chuyển sang trang quản lý kim cương
        public async Task ManageCoins()
        {
            if (IsLoading || !IsViewingSelf)
                return;

            IsLoading = true;

            try
            {
                await _nv.PushModalAsync<DiamondPage>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task Chat()
        {
            // ❌ Không cho chat với chính mình
            if (IsViewingSelf || ViewingUserId == null)
                return;

            var targetUserId = ViewingUserId.Value;

            bool alreadyUnlocked = PreferencesHelper.IsChatUnlocked(targetUserId);

            if (!alreadyUnlocked)
            {
                bool confirm = await ApiResponseHelper.ShowShellConfirmAsync(
                    "Xác nhận mở khóa",
                    "Cần tiêu 130 kim cương 💎 để mở khóa cuộc trò chuyện này. Bạn có muốn tiếp tục không?",
                    "Đồng ý",
                    "Hủy");

                if (!confirm)
                    return;
            }

            var response = await _conversationService.StartConversationAsync(targetUserId);

            if (response.Errors?.Any(e => e.Code == "NotEnoughDiamonds") == true ||
                response.Message?.Contains("kim cương", StringComparison.OrdinalIgnoreCase) == true)
            {
                bool goTopUp = await ApiResponseHelper.ShowShellConfirmAsync(
                    "Không đủ kim cương 💎",
                    "Bạn không đủ kim cương để mở khóa cuộc trò chuyện này. Bạn có muốn nạp thêm không?",
                    "Nạp ngay",
                    "Đóng");

                if (goTopUp)
                {
                    await _nv.PushModalAsync<DiamondPage>();
                }
                return;
            }

            if (response.IsSuccess && response.Data?.ConversationId is Guid conId)
            {
                PreferencesHelper.SetChatUnlocked(targetUserId, true);

                if (!alreadyUnlocked)
                {
                    await _anv.DisplayAlertAsync(
                        "Mở khóa thành công",
                        $"Bạn đã mở khóa cuộc trò chuyện. Số dư còn lại: {response.Data.NewBalance} 💎");
                }

                await _nv.PushModalAsync<MessagePage, MessageNavigationParam>(
                    new MessageNavigationParam
                    {
                        ConversationId = conId,
                        Partner = new UserDiaryModel
                        {
                            Id = CurrentUser!.UserDTO!.Id,
                            FullName = CurrentUser.UserDTO.FullName,
                            AvatarUrl = CurrentUser.UserProfileDTO?.AvatarUrl,
                            Gender = CurrentUser.UserDTO.Gender,
                            IsOnline = false, // hoặc lấy từ PresenceService nếu có
                            IsFollow = IsFollowing
                        }
                    });
            }
            else
            {
                await _res.ShowApiErrorsAsync(response, "Không thể mở chat");
            }
        }
    } 
}