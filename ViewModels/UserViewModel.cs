using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapster;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    internal partial class UserViewModel(IUserService userService, IUserSessionService userSessionService) : ObservableObject
    {
        private readonly IUserService _userService = userService;
        private readonly IUserSessionService _userSession = userSessionService;

        private UserModel? OldUserModel;

        [ObservableProperty]
        public partial bool IsLoading { get; set; }
        [ObservableProperty]
        public partial UserModel? UserModel { get; set; }

        public async Task LoadUserAsync()
        {
            IsLoading = true;
            try
            {
                var result = _userSession.CurrentUser?.UserDTO ?? PreferencesHelper.LoadCurrentUser()?.UserDTO;
                if (result is null)
                {
                    await Shell.Current.DisplayAlert("Lỗi", "Không tìm thấy thông tin người dùng.", "OK");
                }
                UserModel = _userSession.CurrentUser?.UserDTO?.Adapt<UserModel>();
                OldUserModel = UserModel?.Adapt<UserModel>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task UpdateProfileAsync()
        {
            if (UserModel is null || OldUserModel is null)
                return;
            var patchData = new List<JsonPatchOperation>();
            if (string.IsNullOrWhiteSpace(UserModel?.FullName))
            {
                await Shell.Current.DisplayAlert("Thông báo", "Họ và tên không được trống", "OK");
                return;
            }

            if (UserModel.FullName != OldUserModel?.FullName)
                patchData.Add(new JsonPatchOperation { Path = "/fullName", Value = UserModel.FullName });

            if (UserModel?.Gender != null && UserModel?.Gender != OldUserModel?.Gender)
                patchData.Add(new JsonPatchOperation { Path = "/gender", Value = UserModel?.Gender });

            if (UserModel?.BirthDay != null)
            {
                var birthday = UserModel.BirthDay.Value;
                var today = DateTime.Today;

                if (birthday > today)
                {
                    await Shell.Current.DisplayAlert("Thông báo", "Ngày sinh không thể là ngày trong tương lai.", "OK");
                    return;
                }

                if (birthday > today.AddYears(-10))
                {
                    await Shell.Current.DisplayAlert("Thông báo", "Bạn phải từ 10 tuổi trở lên để đăng ký.", "OK");
                    return;
                }

                var formattedDate = UserModel?.GetFormattedBirthDateUtc();
                if (formattedDate != OldUserModel?.GetFormattedBirthDateUtc())
                    patchData.Add(new JsonPatchOperation { Path = "/birthDay", Value = formattedDate });
            }

            if (patchData.Count == 0)
            {
                await Shell.Current.DisplayAlert("Thông báo", "Bạn chưa thay đổi thông tin nào để cập nhật.", "OK");
                return;
            }
            if (IsLoading) return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

            try
            {
                var response = await _userService.UpdateProfileAsync(UserModel!.Id, patchData);

                if (response.Data is not null)
                {
                    if (_userSession.CurrentUser is not null)
                    {
                        // Cập nhật lại DTO từ kết quả patch
                        _userSession.CurrentUser.UserDTO = response.Data;
                        // Gán lại để trigger OnCurrentUserChanged trong ProfileViewModel cập nhật
                        _userSession.CurrentUser = _userSession.CurrentUser.Adapt<UserWithUserProfileModel>();
                        PreferencesHelper.SaveCurrentUser(_userSession.CurrentUser);
                    }

                    KeyboardService.HideKeyboard();
                    await Shell.Current.Navigation.PopModalAsync();
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response!, "Cập nhật thất bại");
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }
    }
}