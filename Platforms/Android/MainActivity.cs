using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Views;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls;
using Sphere.Services.IService;
using Sphere.Services.Service;


namespace Sphere.Platforms.Android
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity? Instance { get; private set; }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            bool granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
            MainActivityHelpers.PermissionCallback?.Invoke(granted);
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 998 || requestCode == 999)
            {
                ImagePickerService.Instance?.OnActivityResult(requestCode, resultCode, data);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Thông báo PermissionService
            PermissionService.Instance?.NotifyReturnedFromSettings();
        }
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Instance = this;

            
            Window?.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateHidden);
           

            DependencyService.Register<IImagePickerService, ImagePickerService>();

        }


    }
}