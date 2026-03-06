using CommunityToolkit.Maui.Views;
using Android.Content;
using Android.Net;
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
    public partial class LoginViewModel(IServiceProvider serviceProvider, IAuthService authService, IUserSessionService userSession, IUserProfileService userProfileService, IPermissionService permissionService, ILocationService locationService, IShellNavigationService nv, PresenceService presence, IAppNavigationService anv, ApiResponseHelper res) : ObservableObject
    {
        private readonly IAuthService _authService = authService;

        private readonly ILocationService _locationService = locationService;
        private readonly IPermissionService _permissionService = permissionService;
        //private readonly PresenceService _presence = presence;
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
        public Task TogglePassword()
        {
            IsPasswordVisible = !IsPasswordVisible;
            OnPropertyChanged(nameof(PasswordIcon));
            return Task.CompletedTask;
        }

        [RelayCommand]
        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(LoginModel.PhoneNumber))
            {
                await _anv.DisplayAlertAsync("Thông báo", "Vui lòng nhập số điện thoại.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LoginModel.Password))
            {
                await _anv.DisplayAlertAsync("Thông báo", "Vui lòng nhập mật khẩu.");
                return;
            }

            if (IsLoading) return;

            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();

            try
            {
                var response = await _authService.LoginAsync(LoginModel);

                if (!response.IsSuccess)
                {
                    await res.ShowApiErrorsAsync(response, "Đăng nhập thất bại");
                    return;
                }

                SaveTokens(response.Data!);

                var profile = await _userProfileService.GetUserProfileMeAsync();

                if (!profile.IsSuccess)
                {
                    await _authService.LogoutAsync();
                    await res.ShowApiErrorsAsync(profile, "Không lấy được hồ sơ người dùng");
                    return;
                }

                await SetupUserSessionAsync(profile.Data!);

                if (!PreferencesHelper.HasSeenIntro())
                {
                    await ShowIntroAsync();
                }
                else
                {
                    await InitAppAsync();   
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }
        private void SaveTokens(TokenResponse data)
        {
            PreferencesHelper.SetAuthToken(data.Token);
            PreferencesHelper.SetRefreshToken(data.RefreshToken);
            PreferencesHelper.SetRefreshTokenId(data.RefreshTokenId.ToString());
            PreferencesHelper.SetAuthTokenExpiresAt(DateTime.UtcNow.AddSeconds(data.ExpiresIn));
        }
        private async Task SetupUserSessionAsync(UserWithUserProfileModel user)
        {
            _userSession.CurrentUser = user;
            PreferencesHelper.SaveCurrentUser(user);

            KeyboardService.HideKeyboard();

            var convVm = _serviceProvider.GetService<ConversationsViewModel>();
            if (convVm != null)
                await convVm.ClearConversationsAsync();
        }
        private async Task ShowIntroAsync()
        {
            var intro = _serviceProvider.GetRequiredService<IntroPage>();

            intro.OnFinishedIntro = async () =>
            {
                PreferencesHelper.SetIntroShown();
                await InitAppAsync();
            };

            _anv.SetRootPage(new NavigationPage(intro));
        }
        private async Task InitAppAsync()
        {
            //var shell = _serviceProvider.GetRequiredService<AppShell>();

            await MainThread.InvokeOnMainThreadAsync(() => { _anv.SetRootPage(new AppShell(_serviceProvider, _authService, _permissionService, presence, _anv, res)); });

            await Task.Delay(200);

            await InitializeServicesAsync();
            
        }
        private async Task RequestInitialPermissionsAsync()
        {
            try
            {
                var result = await _permissionService.RequestPermissionAsync(AppPermission.Location);

                switch (result)
                {
                    case PermissionResult.Granted:
                        await UpdateUserLocationAsync();
                        break;

                    case PermissionResult.Denied:
                        // user từ chối lần 1 -> không làm gì
                        break;

                    case PermissionResult.DeniedDontAskAgain:
                        // dialog mở settings đã xử lý trong PermissionService
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Permission error: {ex.Message}");
            }
        }
        private async Task InitializeServicesAsync()
        {
            try
            {
                var presenceService = _serviceProvider.GetRequiredService<PresenceService>();
                var userId = _userSession.CurrentUser!.UserProfileDTO!.Id;
                await presenceService.StartAsync(userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Init services error: {ex.Message}");
            }
            await RequestInitialPermissionsAsync();
        }
        
        private async Task UpdateUserLocationAsync()
        {
            try
            {
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
                await _anv.DisplayAlertAsync("Lỗi", $"Có lỗi xảy ra khi lấy hoặc gửi vị trí: {ex.Message}");
            }
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

            if (locs.Count == 0) return null;

            // Lấy trung bình để giảm sai số
            double avgLat = locs.Average(l => l.Latitude);
            double avgLon = locs.Average(l => l.Longitude);

            return new Location(avgLat, avgLon);
        }

        [RelayCommand]
        public async Task Register()
        {
            
            await _nv.PushModalAsync<RegisterPage>();
        }

        [RelayCommand]
        public async Task ForgotPassword()
        {
            await _nv.PushModalAsync<ForgotPasswordPage>();
        }
    }
}