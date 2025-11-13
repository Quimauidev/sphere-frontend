using Android;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Sphere.Common.Constans;
using Sphere.Extensions;
using Sphere.Platforms.Android;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    public class PermissionService : IPermissionService
    {
        private TaskCompletionSource<bool>? _tcs;
        private const string ReadMediaImages = "android.permission.READ_MEDIA_IMAGES";

        // 🔔 Event khi người dùng bật GPS xong quay lại app
        // 🧠 Singleton để MainActivity và ViewModel có thể truy cập
        private bool _justReturnedFromSettings; // ⚡️Flag tránh popup lặp lại
        private bool _gpsDialogOpen = false;

        public static PermissionService Instance { get; private set; } = null!;

        // 🔁 Sự kiện sẽ bắn ra khi người dùng quay lại từ Settings
        public event Action? ReturnedFromSettings;

        public PermissionService()
        {
            Instance = this;
        }

        public async Task<bool> EnsureGrantedAsync(AppPermission permissionType)
        {
            string permission = permissionType switch
            {
                AppPermission.ReadImages => Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
                    ? ReadMediaImages
                    : Manifest.Permission.ReadExternalStorage,
                AppPermission.Camera => Manifest.Permission.Camera,
                AppPermission.Location => Manifest.Permission.AccessFineLocation,
                AppPermission.Microphone => Manifest.Permission.RecordAudio,
                _ => throw new NotSupportedException()
            };

            var activity = Platform.CurrentActivity!;

            // 1️⃣ Kiểm tra quyền
            if (ContextCompat.CheckSelfPermission(activity, permission) != Permission.Granted)
            {
                var prefKey = $"FirstRequest_{permission}";
                bool isFirstRequest = Preferences.Get(prefKey, true);

                if (isFirstRequest)
                {
                    Preferences.Set(prefKey, false);
                    _tcs = new TaskCompletionSource<bool>();
                    MainActivityHelpers.PermissionCallback = granted => _tcs?.TrySetResult(granted);

                    ActivityCompat.RequestPermissions(activity, new[] { permission }, 1234);
                    if (!await _tcs.Task)
                        return false;
                }
                else if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, permission))
                {
                    _tcs = new TaskCompletionSource<bool>();
                    MainActivityHelpers.PermissionCallback = granted => _tcs?.TrySetResult(granted);

                    ActivityCompat.RequestPermissions(activity, new[] { permission }, 1234);
                    if (!await _tcs.Task)
                        return false;
                }
                else
                {
                    bool openSettings = await Application.Current!.MainPage!.DisplayAlert(
                        "Cần cấp quyền",
                        $"Ứng dụng cần quyền truy cập {permissionType.ToFriendlyName()}.",
                        "Mở cài đặt",
                        "Hủy");

                    if (openSettings)
                    {
                        var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                        intent.SetData(Android.Net.Uri.FromParts("package", activity.PackageName, null));
                        activity.StartActivity(intent);
                    }
                    return false;
                }
            }

            // 2️⃣ Kiểm tra GPS hoạt động thật nếu là quyền Location
            if (permissionType == AppPermission.Location)
            {
                bool gpsOk = await CheckGpsStatusAsync();
                if (!gpsOk)
                    return false;
            }

            return true;
        }

        public async Task<bool> CheckGpsStatusAsync()
        {
            // ⏳ Nếu vừa trở lại từ Settings → đợi hệ thống cập nhật GPS
            if (_justReturnedFromSettings)
            {
                _justReturnedFromSettings = false;
                await Task.Delay(1500);
            }
            var activity = Platform.CurrentActivity!;
            var lm = (LocationManager?)activity.GetSystemService(Context.LocationService);
            if (lm == null)
                return false;

            bool isGpsEnabled = lm.IsProviderEnabled(LocationManager.GpsProvider)
                             || lm.IsProviderEnabled(LocationManager.NetworkProvider);

            if (isGpsEnabled)
                return true; // ✅ GPS đang bật
            if (_gpsDialogOpen)
                return false; // ❗ Không mở thêm popup nếu đã có
            _gpsDialogOpen = true;

            // 🔹 GPS đang tắt → chỉ hiển thị cảnh báo ngắn gọn rồi mở Settings
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {


                    bool open = await Application.Current!.MainPage!.DisplayAlert(
                        "GPS đang tắt",
                        "Bạn cần bật GPS để tìm quanh đây.",
                        "Mở cài đặt",
                        "Hủy");

                    if (open)
                    {
                        try
                        {
                            var intent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                            intent.SetFlags(ActivityFlags.NewTask);
                            activity.StartActivity(intent);
                        }
                        catch
                        {
                            await Application.Current!.MainPage!.DisplayAlert("Lỗi", "Không thể mở cài đặt GPS.", "OK");
                        }
                    }
                }
                finally
                {
                    _gpsDialogOpen = false;
                }
            });

            // ❌ Không chờ kết quả bật GPS — để OnResume xử lý tiếp
            return false;
        }


        public void NotifyReturnedFromSettings()
        {
            // ✅ Gọi khi quay lại từ Settings (trong MainActivity.OnResume)
            _justReturnedFromSettings = true;
            // ✅ Đảm bảo đóng popup GPS nếu đang mở
            _gpsDialogOpen = false;
            // 🔥 GPS đã bật → bắn event
            ReturnedFromSettings?.Invoke();
        }
    }
}