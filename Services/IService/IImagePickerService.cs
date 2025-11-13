using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IImagePickerService
    {
        // Trả về danh sách đường dẫn file ảnh local (local file path hoặc URI)

        Task<string?> PickSingleImageAsync();
        Task<string> CopyContentUriToTempFileAsync(Android.Net.Uri uri);

        void OnActivityResult(int requestCode, Result resultCode, Intent? data);
    }

}