using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Models.AuthModel;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Controls;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class LoginViewModel(IServiceProvider serviceProvider, IAuthService authService, IUserSessionService userSession, IUserProfileService userProfileService, IPermissionService permissionService, ILocationService locationService) : ObservableObject
    {
        private readonly IAuthService _authService = authService;

        private readonly ILocationService _locationService = locationService;
        private readonly IPermissionService _permissionService = permissionService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private readonly IUserProfileService _userProfileService = userProfileService;

        private readonly IUserSessionService _userSession = userSession;
        private bool _isCheckingGps;

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial bool IsPasswordVisible { get; set; } = false;

        [ObservableProperty]
        public partial LoginModel LoginModel { get; set; } = new();

        // Khởi tạo mặc định
        public string PasswordIcon => IsPasswordVisible ? "\U000F0209" : "\U000F0208";

        [RelayCommand]
        public async Task ForgotPassword()
        {
            var forgotPassword = _serviceProvider.GetRequiredService<ForgotPasswordPage>();
            await Application.Current!.MainPage!.Navigation.PushModalAsync(forgotPassword);
        }

        [RelayCommand]
        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(LoginModel.PhoneNumber))
            {
                await ShowAlertAsync("Thông báo", "Vui lòng nhập số điện thoại.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LoginModel.Password))
            {
                await ShowAlertAsync("Thông báo", "Vui lòng nhập mật khẩu.");
                return;
            }

            if (IsLoading) return;
            IsLoading = true;
            PopupHelper.ShowLoadingAsync();
            try
            {
                var response = await _authService.LoginAsync(LoginModel);
                if (response.IsSuccess)
                {
                    PreferencesHelper.SetAuthToken(response.Data!.Token);
                    PreferencesHelper.SetRefreshToken(response.Data.RefreshToken);
                    PreferencesHelper.SetRefreshTokenId(response.Data.RefreshTokenId.ToString());
                    PreferencesHelper.SetAuthTokenExpiresAt(DateTime.UtcNow.AddSeconds(response.Data.ExpiresIn));
                    // Gọi API lấy profile (user + userprofile)
                    var profile = await _userProfileService.GetUserProfileMeAsync();

                    if (profile.IsSuccess)
                    {
                        // Gán vào session
                        _userSession.CurrentUser = profile.Data!;

                        PreferencesHelper.SaveCurrentUser(profile.Data!);

                        KeyboardService.HideKeyboard();

                        // Sau khi gán CurrentUser và Preferences
                        var newUserId = profile.Data!.UserProfileDTO!.Id;

                        // Tạo instance mới PresenceService
                        var presenceService = new PresenceService("https://sphere-iqm8.onrender.com", newUserId, _serviceProvider);
                        await presenceService.StartAsync();
                        if (!PreferencesHelper.HasSeenIntro())
                        {
                            var intro = _serviceProvider.GetRequiredService<IntroPage>();
                            intro.OnFinishedIntro = async () =>
                            {
                                Application.Current!.MainPage = new AppShell(_serviceProvider, _authService, _permissionService, _locationService, presenceService);
                                await Task.Delay(200);

                                _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
                                _permissionService.ReturnedFromSettings += OnReturnedFromSettings;
                            };
                            Application.Current!.MainPage = new NavigationPage(intro);
                        }
                        else
                        {
                            Application.Current!.MainPage = new AppShell(_serviceProvider, _authService, _permissionService, _locationService, presenceService);
                            _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
                            _permissionService.ReturnedFromSettings += OnReturnedFromSettings;
                        }
                    }
                    else
                    {
                        await _authService.LogoutAsync();
                        await ApiResponseHelper.ShowApiErrorsAsync(profile, "Không lấy được hồ sơ người dùng");
                    }
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response, "Đăng nhập thất bại");
                }
            }
            finally
            {
                PopupHelper.HideLoading();
                IsLoading = false;
            }
        }
        [RelayCommand]
        public async Task Register()
        {
            if (IsLoading) return;
            var register = _serviceProvider.GetRequiredService<RegisterPage>();
            await Application.Current!.MainPage!.Navigation.PushModalAsync(register);
        }

        [RelayCommand]
        public Task TogglePassword()
        {
            IsPasswordVisible = !IsPasswordVisible;
            OnPropertyChanged(nameof(PasswordIcon));
            return Task.CompletedTask;
        }

        private static async Task ShowAlertAsync(string title, string message)
                => await Application.Current!.MainPage!.DisplayAlert(title, message, "OK");

        private async Task<Location?> GetStableLocationAsync(int samples = 5, int maxAccuracyMeters = 50)
        {
            var locs = new List<Location>();
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));

            for (int i = 0; i < samples; i++)
            {
                try
                {
                    var loc = await Geolocation.Default.GetLocationAsync(request);
                    if (loc != null && loc.Accuracy <= maxAccuracyMeters)
                        locs.Add(loc);

                    await Task.Delay(500); // đợi GPS fix lại
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi lấy GPS: {ex.Message}");
                    await Task.Delay(500);
                }
            }

            if (!locs.Any()) return null;

            // Lấy trung bình để giảm sai số
            double avgLat = locs.Average(l => l.Latitude);
            double avgLon = locs.Average(l => l.Longitude);

            return new Location(avgLat, avgLon);
        }

        private async void OnReturnedFromSettings()
        {
            if (_isCheckingGps) return;
            _isCheckingGps = true;

            try
            {
                bool granted = await _permissionService.EnsureGrantedAsync(AppPermission.Location);
                if (granted)
                    await UpdateUserLocationAsync();
            }
            finally
            {
                _isCheckingGps = false;
            }
        }

        private async Task UpdateUserLocationAsync()
        {
            try
            {
                var granted = await _permissionService.EnsureGrantedAsync(AppPermission.Location);
                if (!granted) return;

                var location = await GetStableLocationAsync();
                if (location == null)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Vị trí", "Không lấy được vị trí từ thiết bị.", "OK");
                    return;
                }

                var dto = new UserLocationModel
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    LastUpdated = DateTime.UtcNow,
                    IsVisible = true
                };

                await _locationService.UpdateLocationAsync(dto);
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Lỗi",
                    $"Có lỗi xảy ra khi lấy hoặc gửi vị trí: {ex.Message}",
                    "OK");
            }
        }
    }
}