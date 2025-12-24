using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Extensions;
using Sphere.Models;
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
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly IImagePickerService _imagePickerService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserProfileService _userProfileService;
        private readonly IUserSessionService _userSession;

        [ObservableProperty]
        public partial bool ShouldShowBioToggle { get; set; }
        public DiaryListViewModel DiaryListVM { get; }
        public ProfileViewModel(IUserSessionService userSession, IImagePickerService imagePickerService, IUserProfileService userProfileService, IServiceProvider serviceProvider, IDiaryService diaryService)
        {
            _userSession = userSession;
            _imagePickerService = imagePickerService;
            _userProfileService = userProfileService;
            _serviceProvider = serviceProvider;
            var restoredUser = PreferencesHelper.LoadCurrentUser();
            if (restoredUser != null)
            {
                _userSession.CurrentUser = restoredUser;
            }
            CurrentUser = _userSession.CurrentUser;
            DiaryListVM = new DiaryListViewModel(diaryService);
            _ = DiaryListVM.LoadFirstPage();
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
            var oldBio = CurrentUser?.UserProfileDTO?.Bio ?? string.Empty;
            var popup = new BioEditPopup(oldBio);

            if (await Shell.Current.ShowPopupAsync(popup) is string result)
            {
                var newBio = result.Trim();
                if (newBio == oldBio.Trim())
                {
                    await Shell.Current.DisplayAlert("Thông báo", "Bạn chưa thay đổi nội dung tiểu sử", "OK");
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
            var page = _serviceProvider.GetRequiredService<EditUserProfilePage>();
            if (page.BindingContext is UserViewModel viewModel)
            {
                await viewModel.LoadUserAsync();
            }
            await Shell.Current.Navigation.PushModalAsync(page);
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
                    await ApiResponseHelper.ShowApiErrorsAsync(response!, "Thất bại");
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

            var action = await Shell.Current.DisplayActionSheet(
                $"Tùy chọn {displayName}",
                "Hủy",
                null,
                "Xem ảnh",
                "Chỉnh sửa ảnh",
                "Xóa ảnh");

            switch (action)
            {
                case "Xem ảnh":
                    await Shell.Current.Navigation.PushModalAsync(new ImageViewerPage(imageUrl!));
                    break;

                case "Chỉnh sửa ảnh":
                    await EditImageAsync(imageType);
                    break;

                case "Xóa ảnh":
                    await DeleteImageAsync(imageType);
                    break;

                default:
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
            bool confirm = await Shell.Current.DisplayAlert(
                "Xác nhận",
                "Bạn có chắc muốn xoá ảnh không?",
                "Xoá",
                "Huỷ");

            if (!confirm) return;

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
                    await ApiResponseHelper.ShowApiErrorsAsync(response!, "Không thể xoá ảnh");
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
                    await ApiResponseHelper.ShowApiErrorsAsync(response!, "Thất bại");
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task PostStatus()
        {
            var page = _serviceProvider.GetRequiredService<PostDiaryPage>();
            await Shell.Current.Navigation.PushModalAsync(page);
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
        // chuyeden trang quan ly coin
        public async Task ManageCoins()
        {
            var page = _serviceProvider.GetRequiredService<DiamondPage>();
            await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
        }
    } 
}