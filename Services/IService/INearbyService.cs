using Android.Media.TV;
using Sphere.Common.Responses;
using Sphere.DTOs;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface INearbyService
    {
        Task<ApiResponse<IEnumerable<NearbyModel>>> GetNearbyUsersAsync(NearbyRequest request);
        Task<ApiResponse<object>> UpdateLocationAsync(UpdateLocationRequest request);
        Task<ApiResponse<object>> CreateLocationAsync(CreateLocationRequest request);
        Task<ApiResponse<object>> SetLocationVisibilityAsync(bool isVisible);
    }
}
