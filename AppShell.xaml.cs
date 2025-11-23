using Kotlin.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Hubs;
using Sphere.Models;
using Sphere.Services.IService;
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
            // Khi AppShell load lần đầu, yêu cầu quyền
            RequestLocationPermissionFirstTime();
        }

        private async void RequestLocationPermissionFirstTime()
        {
            // Nếu lần đầu mở app sau Login + Intro
            if (!Preferences.Get("LocationAsked", false))
            {
                Preferences.Set("LocationAsked", true);

                await _permissionService.EnsureGrantedAsync(AppPermission.Location);
            }
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
        
    }
}