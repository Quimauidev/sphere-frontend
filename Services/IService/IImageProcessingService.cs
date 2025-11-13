using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    internal interface IImageProcessingService
    {
        Task<byte[]> ResizeImageToFullHDAsync(string imagePath);
        Task<List<byte[]>> ResizeImagesToFullHDAsync(List<string> imagePaths);
    }
}
