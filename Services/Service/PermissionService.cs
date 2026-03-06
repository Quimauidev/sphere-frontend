using Android;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Extensions;
using Sphere.Platforms.Android;
using Sphere.Services.IService;
using Sphere.ViewModels;
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
        private readonly IAppNavigationService _anv;

        // 🔔 Event khi người dùng bật GPS xong quay lại app
        // 🧠 Singleton để MainActivity và ViewModel có thể truy cập
        private bool _justReturnedFromSettings; // ⚡️Flag tránh popup lặp lại
        private bool _gpsDialogOpen = false;

        public static PermissionService Instance { get; private set; } = null!;

        // 🔁 Sự kiện sẽ bắn ra khi người dùng quay lại từ Settings
        public event Action? ReturnedFromSettings;
        public event Action? GpsTurnedOff;


        public PermissionService(IAppNavigationService anv)
        {
        Instance = this;
        _anv = anv;
    }

        // API chính: RequestPermissionAsync
        public async Task<PermissionResult> RequestPermissionAsync(AppPermission permission)
        {
            var status = await CheckStatusAsync(permission);

            if (status == PermissionResult.Granted)
                return PermissionResult.Granted;
            bool wasRequestedBefore = Preferences.ContainsKey($"permission_{permission}");
            // Nếu trước khi request đã là Don't ask again
            if (status == PermissionResult.DeniedDontAskAgain && wasRequestedBefore)
            {
                await HandleDontAskAgain(permission);
                return PermissionResult.DeniedDontAskAgain;
            }

            // Request permission
            var requestResult = await RequestPermissionAsyncInternal(permission);

            if (requestResult == PermissionResult.Granted)
                return PermissionResult.Granted;

            // Sau khi user từ chối

            return PermissionResult.Denied;
        }

        // Tách: CheckStatus
        private async Task<PermissionResult> CheckStatusAsync(AppPermission permission)
        {
            var activity = Platform.CurrentActivity!; // kiểm tra quyền, request quyền, mở settings đều cần context/activity
            string androidPermission = GetAndroidPermission(permission); // Lấy tên permission Android tương ứng

            var granted = ContextCompat.CheckSelfPermission(activity, androidPermission) == Permission.Granted; // Kiểm tra quyền đã được cấp chưa

            if (granted) // Nếu đã cấp quyền
                return PermissionResult.Granted; // không cần hỏi nữa

            bool rationale = ActivityCompat.ShouldShowRequestPermissionRationale(activity, androidPermission); // Kiểm tra Android có cho hiển thị lại dialog hay không

            // Nếu chưa từng request permission
            bool firstTime = !Preferences.ContainsKey($"permission_{permission}"); // Kiểm tra app đã từng request permission chưa

            if (firstTime) // Nếu lần đầu mở app
                return PermissionResult.Denied; 

            if (!rationale) // Nếu user chọn "Don't ask again"
                return PermissionResult.DeniedDontAskAgain;

            return PermissionResult.Denied;
        }

        // Tách: RequestPermission
        private async Task<PermissionResult> RequestPermissionAsyncInternal(AppPermission permission)
        {
            var activity = Platform.CurrentActivity!;
            string androidPermission = GetAndroidPermission(permission);

            _tcs = new TaskCompletionSource<bool>();

            MainActivityHelpers.PermissionCallback =
                granted => _tcs?.TrySetResult(granted);

            ActivityCompat.RequestPermissions(activity, [androidPermission], 1234);
            var grantedResult = await _tcs.Task;

            Preferences.Set($"permission_{permission}", true);
            return grantedResult
                ? PermissionResult.Granted
                : PermissionResult.Denied;
        }

        // Tách: HandleDontAskAgain
        private async Task HandleDontAskAgain(AppPermission permission)
        {
            var openSettings = await _anv.ShowConfirmAsync(
                "Cần cấp quyền",
                $"Ứng dụng cần quyền này để sử dụng tính năng",
                "Mở cài đặt",
                "Hủy"
            );
            if (openSettings)
            {
                await OpenSettingsAsync();
            }
        }

        // Tách: OpenSettings
        private async Task OpenSettingsAsync()
        {
            var activity = Platform.CurrentActivity!;
            var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.SetData(Android.Net.Uri.FromParts("package", activity.PackageName, null));
            activity.StartActivity(intent);
            await Task.CompletedTask;
        }

        // Helper: mapping permission
        private string GetAndroidPermission(AppPermission permission)
        {
            return permission switch
            {
                AppPermission.ReadImages => Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
                    ? "android.permission.READ_MEDIA_IMAGES"
                    : Manifest.Permission.ReadExternalStorage,
                AppPermission.Camera => Manifest.Permission.Camera,
                AppPermission.Location => Manifest.Permission.AccessFineLocation,
                AppPermission.Microphone => Manifest.Permission.RecordAudio,
                _ => throw new NotSupportedException()
            };
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
                             // ❌ GPS tắt → tắt switch ngay
            if (!isGpsEnabled)
            {
                GpsTurnedOff?.Invoke();
            }

            if (_gpsDialogOpen)
                return false; // ❗ Không mở thêm popup nếu đã có
            _gpsDialogOpen = true;

            // 🔹 GPS đang tắt → chỉ hiển thị cảnh báo ngắn gọn rồi mở Settings
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {


                    bool open = await _anv.ShowConfirmAsync( "GPS đang tắt", "Bạn cần bật GPS để tìm quanh đây.", "Mở cài đặt", "Hủy"); 
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
                            await _anv.DisplayAlertAsync("Lỗi", "Không thể mở cài đặt GPS.");
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

        public bool IsGpsEnabled()
        {
            var lm = (LocationManager?)Platform.CurrentActivity?.GetSystemService(Context.LocationService);
            if (lm == null) return false;
            return lm.IsProviderEnabled(LocationManager.GpsProvider) || lm.IsProviderEnabled(LocationManager.NetworkProvider);
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