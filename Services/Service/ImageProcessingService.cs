using Android.Graphics;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkiaSharp;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sphere.Services.Service
{
    internal class ImageProcessingService : IImageProcessingService
    {
        public async Task<List<byte[]>> ResizeImagesToFullHDAsync(List<string> imagePaths)
        {
            var result = new List<byte[]>();
            foreach (var path in imagePaths)
            {
                var resized = await ResizeImageToFullHDAsync(path);
                result.Add(resized);
            }
            return result;
        }

        public async Task<byte[]> ResizeImageToFullHDAsync(string imagePath)
        {
            return await Task.Run(() => Resize(imagePath));
        }

        private byte[] Resize(string path)
        {
            using var input = File.OpenRead(path);
            using var original = SKBitmap.Decode(input);

            if (original == null)
                throw new Exception("Không thể đọc ảnh");

            // 🔁 Xử lý orientation
            int orientation = GetExifOrientation(path);
            using var rotated = ApplyOrientation(original, orientation);

            // 👇 Giới hạn theo chiều ảnh
            int maxW, maxH;
            if (rotated.Width > rotated.Height)
            {
                maxW = 1920;
                maxH = 1080;
            }
            else
            {
                maxW = 1080;
                maxH = 1920;
            }

            if (rotated.Width <= maxW && rotated.Height <= maxH)
            {
                using var ms = new MemoryStream();
                rotated.Encode(ms, SKEncodedImageFormat.Jpeg, 90);
                return ms.ToArray();
            }

            float ratio = Math.Min((float)maxW / rotated.Width, (float)maxH / rotated.Height);
            int newW = (int)(rotated.Width * ratio);
            int newH = (int)(rotated.Height * ratio);

            // Cách chuẩn mới
            var sampling = new SKSamplingOptions(
                SKFilterMode.Linear,   // FilterMode (Linear = tương đương High)
                SKMipmapMode.None       // MipmapMode
            );

            using var resized = rotated.Resize(
                new SKImageInfo(newW, newH),
                sampling
            );

            if (resized == null)
                throw new Exception("Resize ảnh thất bại");

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            return data.ToArray();
        }

        private int GetExifOrientation(string imagePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(imagePath);
                var subIfdDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (subIfdDirectory?.TryGetInt32(ExifDirectoryBase.TagOrientation, out var orientation) == true)
                    return orientation;
            }
            catch
            {
                // Ignore
            }
            return 1; // Mặc định không xoay
        }

        private SKBitmap ApplyOrientation(SKBitmap bitmap, int orientation)
        {
            return orientation switch
            {
                3 => RotateBitmap(bitmap, 180),
                6 => RotateBitmap(bitmap, 90),
                8 => RotateBitmap(bitmap, 270),
                _ => bitmap.Copy()
            };
        }

        private SKBitmap RotateBitmap(SKBitmap source, int degrees)
        {
            if (degrees % 360 == 0)
                return source.Copy(); // Không cần xoay

            int newWidth = (degrees == 90 || degrees == 270) ? source.Height : source.Width;
            int newHeight = (degrees == 90 || degrees == 270) ? source.Width : source.Height;

            var rotatedBitmap = new SKBitmap(newWidth, newHeight);
            using var canvas = new SKCanvas(rotatedBitmap);

            canvas.Clear(SKColors.White);
            canvas.Translate(newWidth / 2f, newHeight / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-source.Width / 2f, -source.Height / 2f);

            canvas.DrawBitmap(source, 0, 0);
            canvas.Flush();

            return rotatedBitmap;
        }
    }

}
