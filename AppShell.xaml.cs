using Kotlin.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Pages;
using System;

namespace Sphere
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _authService;
        private readonly ILocationService _locationService;
        private readonly IPermissionService _permissionService;
        private readonly PresenceService _presenceService;
        private readonly IServiceProvider _serviceProvider;
        

        public AppShell(IServiceProvider serviceProvider, IAuthService authService, IPermissionService permissionService, ILocationService locationService, PresenceService presenceService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _authService = authService;
            _permissionService = permissionService;
            _locationService = locationService;
            _presenceService = presenceService;
            
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await UpdateUserLocationAsync();
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
                catch
                {
                    await Task.Delay(500);
                }
            }

            if (!locs.Any()) return null;

            // Lấy trung bình để giảm sai số
            double avgLat = locs.Average(l => l.Latitude);
            double avgLon = locs.Average(l => l.Longitude);

            return new Location(avgLat, avgLon);
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Đồng ý", "Hủy");
            if (!confirm) return;
            
            await _presenceService.StopAsync();

            var userSession = _serviceProvider.GetRequiredService<IUserSessionService>();
            var response = await _authService.LogoutAsync();
            if (!response.IsSuccess)
                await ApiResponseHelper.ShowApiErrorsAsync(response, "Đăng xuất thất bại");
            var login = _serviceProvider.GetRequiredService<LoginPage>();
            
               Application.Current!.MainPage = new NavigationPage(login);
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
                    await Application.Current!.MainPage!.DisplayAlert("Vị trí", "Không lấy được vị trí từ thiết bị", "OK");
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