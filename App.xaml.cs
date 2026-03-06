using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Database.ServiceSQLite;
using Sphere.Hubs;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Pages;

namespace Sphere
{
    public partial class App : Application
    {
        private PresenceService? _presenceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;
        private readonly IAppNavigationService _anv;
        private readonly ApiResponseHelper _res;

        public App(IServiceProvider serviceProvider, IAuthService authService, IPermissionService permissionService, IAppNavigationService anv, ApiResponseHelper res)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _authService = authService;
            _permissionService = permissionService;
            _anv = anv;
            _res = res;
        }

        // Override CreateWindow to provide initial window and handle any pending root page request.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Initial splash UI (replaces previous `MainPage = new ContentPage { ... }` usage).
            var splash = new ContentPage
            {
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            // chạy async sau khi UI đã có
            
            var window = new Window(splash);

            // chạy async sau khi UI đã có
            _ = InitializeAppAsync();

            return window;
        }

        private async Task InitializeAppAsync()
        {
            // ① Khởi tạo SQLite table trước khi làm gì khác
            var initializer = _serviceProvider.GetRequiredService<BaseSQLiteService>();
            await initializer.InitAsync(); // đồng bộ để đảm bảo table đã tồn tại
            string? token = PreferencesHelper.GetAuthToken(); // lấy token từ Preferences
            var expiresAt = PreferencesHelper.GetAuthTokenExpiresAt(); // lấy thời gian hết hạn token từ Preferences
            var isExpired = expiresAt.HasValue && DateTime.UtcNow > expiresAt.Value; // kiểm tra nếu token đã hết hạn

            if (!string.IsNullOrEmpty(token) && !isExpired)
            {
                var restoredUser = PreferencesHelper.LoadCurrentUser();
                if (restoredUser != null)
                {
                    var userSession = _serviceProvider.GetRequiredService<IUserSessionService>();
                    userSession.CurrentUser = restoredUser;

                    _presenceService = new PresenceService( "https://sphere-iqm8.onrender.com", restoredUser.UserProfileDTO!.Id, _serviceProvider, _anv);
                    _ = _presenceService.StartAsync();

                    if (!PreferencesHelper.HasSeenIntro())
                    {
                        var intro = _serviceProvider.GetRequiredService<IntroPage>();
                        intro.OnFinishedIntro = async () =>
                        {
                            // Use helper to change root page
                            await MainThread.InvokeOnMainThreadAsync(() => { _anv.SetRootPage(new AppShell(_serviceProvider, _authService, _permissionService, _presenceService, _anv, _res)); });
                        };

                        _anv.SetRootPage(new NavigationPage(intro));
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => { _anv.SetRootPage(new AppShell( _serviceProvider, _authService, _permissionService, _presenceService, _anv, _res)); });
                    }

                    return;
                }
            }
            var login = _serviceProvider.GetRequiredService<LoginPage>();
            // Nếu không có token hoặc hết hạn → chuyển sang LoginPage
            _anv.SetRootPage(new NavigationPage(login));
        }        
    }
}