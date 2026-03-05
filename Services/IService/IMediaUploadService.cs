using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IMediaUploadService
    {
        Task<List<string>> ResizeAndUploadImagesAsync(List<string> localImagePaths);
        Task<string> ResizeAndUploadSingleImageAsync(string imagePath);
    }
}
