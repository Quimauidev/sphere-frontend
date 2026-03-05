using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Java.Security;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models.AuthModel;
using Sphere.Services.IService;
using Sphere.Views.Controls;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sphere.ViewModels
{
    public partial class RegisterViewModel(IAuthService authService, IShellNavigationService nv) : ObservableObject
    {
        private readonly IAuthService _authService = authService;
        private readonly IShellNavigationService _nv = nv;

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial bool IsPasswordVisible { get; set; }
        public string PasswordIcon => IsPasswordVisible ? "\U000F0209" : "\U000F0208";

        [ObservableProperty]
        public partial RegisterModel RegisterModel { get; set; } = new()
        {
            Gender = Gender.Male,
            BirthDay = DateTime.Today
        };
        [RelayCommand]
        public Task TogglePassword()
        {
            IsPasswordVisible = !IsPasswordVisible;
            OnPropertyChanged(nameof(PasswordIcon));
            return Task.CompletedTask;
        }

        

        [RelayCommand]
        private async Task RegisterAsync()
        {
            // Sử dụng phương thức GetFormattedBirthDate để lấy ngày sinh đã chuyển đổi
            var validationError = ValidateRegister();

            if (!string.IsNullOrEmpty(validationError))
            {
                await ApiResponseHelper.ShowAlertAsync(validationError);
                return;
            }

            _ = RegisterModel.GetFormattedBirthDate(); // Định dạng ISO 8601
            if (IsLoading) return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();


            try
            {
                var response = await _authService.RegisterAsync(RegisterModel);

                if (response.IsSuccess)
                {
                    await ApiResponseHelper.ShowApiSuccessAsync(response, "Thành công");
                    await _nv.PopModalAsync();
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response, "Thất bại");
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;  // Dừng loading
            }
        }
        private string? ValidateRegister()
        {
            if (string.IsNullOrWhiteSpace(RegisterModel.FullName))
                return "Vui lòng nhập Họ và Tên.";

            var birthday = RegisterModel.BirthDay;
            var today = DateTime.Today;

            if (birthday > today)
                return "Ngày sinh không thể là ngày trong tương lai.";

            if (birthday > today.AddYears(-15))
                return "Bạn phải từ 16 tuổi trở lên để đăng ký.";

            if (string.IsNullOrWhiteSpace(RegisterModel.PhoneNumber))
                return "Vui lòng nhập số điện thoại.";

            if (string.IsNullOrWhiteSpace(RegisterModel.Password) ||
                string.IsNullOrWhiteSpace(RegisterModel.ConfirmPassword))
                return "Vui lòng nhập mật khẩu.";

            if (RegisterModel.Password.Length < 6 ||
                RegisterModel.ConfirmPassword.Length < 6)
                return "Mật khẩu có ít nhất 6 ký tự.";

            if (RegisterModel.Password != RegisterModel.ConfirmPassword)
                return "Mật khẩu không khớp.";

            if (!Regex.IsMatch(RegisterModel.FullName, @"^[a-zA-ZÀ-ỹ0-9\s]+$"))
                return "Họ và tên không được chứa ký tự đặc biệt.";

            return null;
        }
    }
}