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
        private readonly ApiResponseHelper _res;
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;
        private readonly PresenceService _presenceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppNavigationService _anv;
        private bool IsLoading;

        public AppShell(IServiceProvider serviceProvider, IAuthService authService, IPermissionService permissionService,  PresenceService presenceService, IAppNavigationService anv, ApiResponseHelper res)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _authService = authService;
            _permissionService = permissionService;
            _presenceService = presenceService;
            _anv = anv;
            _res = res;
            // Khi AppShell load lần đầu, yêu cầu quyền
            RequestLocationPermissionFirstTime();
            
        }

        private async void RequestLocationPermissionFirstTime()
        {
            // Nếu lần đầu mở app sau Login + Intro
            if (!Preferences.Get("LocationAsked", false))
            {
                Preferences.Set("LocationAsked", true);
                await _permissionService.RequestPermissionAsync(AppPermission.Location);
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            if (IsLoading)
                return;
            bool confirm = await ApiResponseHelper.ShowShellConfirmAsync("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất?", "Đồng ý", "Hủy");
            if (!confirm)
                return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();
            try
            {
                await _presenceService.StopAsync();

                var response = await _authService.LogoutAsync();

                if (!response.IsSuccess)
                {
                    await _res.ShowApiErrorsAsync(response, "Đăng xuất thất bại");
                    return;
                }

                var login = _serviceProvider.GetRequiredService<LoginPage>();

                _anv.SetRootPage(new NavigationPage(login));
            }
            finally
            {
                // chuẩn thứ tự
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }

        }
        
    }
}