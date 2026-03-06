using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Hubs;
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
    public partial class LoginViewModel(IServiceProvider serviceProvider, IAuthService authService, IUserSessionService userSession, IUserProfileService userProfileService, IPermissionService permissionService, ILocationService locationService, IShellNavigationService nv, IAppNavigationService anv, ApiResponseHelper res) : ObservableObject
    {
        private readonly IAuthService _authService = authService;

        private readonly ILocationService _locationService = locationService;
        private readonly IPermissionService _permissionService = permissionService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IUserProfileService _userProfileService = userProfileService;
        private readonly IUserSessionService _userSession = userSession;
        private readonly IShellNavigationService _nv = nv;
        private readonly IAppNavigationService _anv = anv;
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
            await _nv.PushModalAsync<ForgotPasswordPage>();
        }

        [RelayCommand]
        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(LoginModel.PhoneNumber))
            {
                await _anv.DisplayAlertAsync("Thông báo","Vui lòng nhập số điện thoại.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LoginModel.Password))
            {
                await _anv.DisplayAlertAsync("Thông báo","Vui lòng nhập mật khẩu.");
                return;
            }

            if (IsLoading) return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

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

                        // Xóa dữ liệu conversation cũ
                        var convVm = _serviceProvider.GetService<ConversationsViewModel>();
                        if (convVm != null)
                            await convVm.ClearConversationsAsync();

                        // Tạo instance mới PresenceService
                        var presenceService = new PresenceService("https://sphere-iqm8.onrender.com", newUserId, _serviceProvider, _anv);
                        await presenceService.StartAsync(); // online ngay sau khi login    
                        if (!PreferencesHelper.HasSeenIntro())
                        {
                            var intro = _serviceProvider.GetRequiredService<IntroPage>();
                            intro.OnFinishedIntro = async () =>
                            {
                                _anv.SetRootPage(new AppShell(_serviceProvider, _authService, _permissionService,  presenceService, _anv, res));
                                await Task.Delay(200);

                                _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
                            };
                            _anv.SetRootPage(new NavigationPage(intro));
                        }
                        else
                        {
                            _anv.SetRootPage(new AppShell(_serviceProvider, _authService, _permissionService,  presenceService, _anv, res));
                            _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
                        }
                    }
                    else
                    {
                        await _authService.LogoutAsync();
                        await res.ShowApiErrorsAsync(profile, "Không lấy được hồ sơ người dùng");
                    }
                }
                else
                {
                    await res.ShowApiErrorsAsync(response, "Đăng nhập thất bại");
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }
        [RelayCommand]
        public async Task Register()
        {
            if (IsLoading) return;
            await _nv.PushModalAsync<RegisterPage>();
        }

        [RelayCommand]
        public Task TogglePassword()
        {
            IsPasswordVisible = !IsPasswordVisible;
            OnPropertyChanged(nameof(PasswordIcon));
            return Task.CompletedTask;
        }

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
                    await _anv.DisplayAlertAsync("Vị trí", "Không lấy được vị trí từ thiết bị.");
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
                await _anv.DisplayAlertAsync( "Lỗi", $"Có lỗi xảy ra khi lấy hoặc gửi vị trí: {ex.Message}");
            }
        }
    }
}