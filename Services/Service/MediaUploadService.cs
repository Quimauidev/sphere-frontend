using Android.Net;
using Java.Nio.FileNio;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sphere.Services.Service
{
    internal class MediaUploadService(IImageProcessingService imageProcessingService, IApiService apiService) : IMediaUploadService
    {
        public async Task<List<string>> ResizeAndUploadImagesAsync(List<string> paths)
        {
            var resizedImages = await imageProcessingService.ResizeImagesToFullHDAsync(paths);
            List<string> tempFilePaths = [];

            foreach (var imgBytes in resizedImages)
            {
                var tempPath = System.IO.Path.Combine(Microsoft.Maui.Storage.FileSystem.CacheDirectory, $"{Guid.NewGuid()}.jpg");
                await File.WriteAllBytesAsync(tempPath, imgBytes);
                tempFilePaths.Add(tempPath);
            }

            return tempFilePaths;
        }

        public async Task<string> ResizeAndUploadSingleImageAsync(string imagePath)
        {
            var resizedImageBytes = await imageProcessingService.ResizeImageToFullHDAsync(imagePath);

            var result = await apiService.UploadImageAsync(resizedImageBytes);
            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Data))
            {
                return result.Data;
            }

            throw new Exception("Tải ảnh thất bại");
        }
    }
}
