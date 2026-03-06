using Android.App;
using Android.Content;
using Android.Provider;
using Sphere.Common.Constans;
using Sphere.Services.IService;

[assembly: Dependency(typeof(Sphere.Platforms.Android.ImagePickerService))]

namespace Sphere.Platforms.Android
{
    internal class ImagePickerService : IImagePickerService
    {
        public static ImagePickerService? Instance { get; private set; }
        public TaskCompletionSource<List<string>>? _taskCompletionSource;
        public IPermissionService _permissionService;

        public ImagePickerService(IPermissionService permissionService)
        {
            Instance = this;
            _permissionService = permissionService;
        }

        public async Task<string?> PickSingleImageAsync()
        {
            var permissionResult = await _permissionService.RequestPermissionAsync(AppPermission.ReadImages);
            if (permissionResult != PermissionResult.Granted) return null;

            _taskCompletionSource = new TaskCompletionSource<List<string>>();

            Intent intent = new(Intent.ActionPick);
            intent.SetType("image/*");
            string[] mimeTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
            intent.PutExtra(Intent.ExtraMimeTypes, mimeTypes);
            intent.PutExtra(Intent.ExtraAllowMultiple, false); // QUAN TRỌNG: chỉ chọn 1 ảnh
            MainActivity.Instance?.StartActivityForResult(Intent.CreateChooser(intent, "Chọn một ảnh"), 998);
            var results = await _taskCompletionSource.Task;
            return results.FirstOrDefault(); // Trả về duy nhất 1 ảnh
        }

        public async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (_taskCompletionSource == null)
            {
                return;
            }
            List<string> selectedImages = [];
            _ = Platform.CurrentActivity!;

            if (data?.ClipData != null && data.ClipData.ItemCount > 0)
            {
                for (int i = 0; i < data.ClipData.ItemCount; i++)
                {
                    var uri = data.ClipData.GetItemAt(i)?.Uri;
                    if (uri != null)
                    {
                        var tempPath = await CopyContentUriToTempFileAsync(uri);
                        selectedImages.Add(tempPath);
                    }
                }
            }
            else if (data?.Data != null) // Chỉ chọn 1 ảnh
            {
                var uri = data.Data;
                var tempPath = await CopyContentUriToTempFileAsync(uri);
                selectedImages.Add(tempPath);
            }
            // Nếu không có ảnh nào, trả về danh sách rỗng
            _taskCompletionSource?.TrySetResult(selectedImages);
        }

        public async Task<string> CopyContentUriToTempFileAsync(global::Android.Net.Uri uri)
        {
            var context = Platform.CurrentActivity!;
            using var input = context.ContentResolver?.OpenInputStream(uri)!;

            var cacheDir = context.CacheDir!.AbsolutePath;
            var fileName = $"temp_{Guid.NewGuid()}.jpg";
            var tempFilePath = Path.Combine(cacheDir, fileName);

            using var output = File.Create(tempFilePath);
            await input.CopyToAsync(output);

            return tempFilePath;
        }
    }
}