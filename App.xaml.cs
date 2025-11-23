using Sphere.Common.Helpers;
using Sphere.Hubs;
using Sphere.Services.IService;
using Sphere.Views.Pages;

namespace Sphere
{
    public partial class App : Application
    {
        private PresenceService? _presenceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthService _authService;
        private readonly IPermissionService _permissionService;
        private readonly ILocationService _locationService;

        public App(IServiceProvider serviceProvider, IAuthService authService, IPermissionService permissionService, ILocationService locationService)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _authService = authService;
            _permissionService = permissionService;
            _locationService = locationService;

            // Gán MainPage tạm thời (Splash / Loading)
            MainPage = new ContentPage
            {
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                },
            };

            InitializeAppAsync();
        }

        private async void InitializeAppAsync()
        {
            string? token = PreferencesHelper.GetAuthToken();
            var expiresAt = PreferencesHelper.GetAuthTokenExpiresAt();
            var isExpired = expiresAt.HasValue && DateTime.UtcNow > expiresAt.Value;

            if (!string.IsNullOrEmpty(token) && !isExpired)
            {
                var restoredUser = PreferencesHelper.LoadCurrentUser();
                if (restoredUser != null)
                {
                    var userSession = _serviceProvider.GetRequiredService<IUserSessionService>();
                    userSession.CurrentUser = restoredUser;

                    _presenceService = new PresenceService(
                        "https://sphere-iqm8.onrender.com",
                        restoredUser.UserProfileDTO!.Id,
                        _serviceProvider
                    );
                    await _presenceService.StartAsync();
                    if (!PreferencesHelper.HasSeenIntro())
                    {
                        var intro = _serviceProvider.GetRequiredService<IntroPage>();
                        intro.OnFinishedIntro = () =>
                        {
                            MainPage = new AppShell(_serviceProvider, _authService, _permissionService, _locationService, _presenceService);
                        };
                        MainPage = new NavigationPage(intro);
                    }
                    else
                    {
                        MainPage = new AppShell(_serviceProvider, _authService, _permissionService, _locationService, _presenceService);
                    }

                    return;
                }
            }

            // Nếu không có token hoặc hết hạn → chuyển sang LoginPage
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new NavigationPage(_serviceProvider.GetRequiredService<LoginPage>());
            });
        }
    }
}