using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Sphere.DTOs;

namespace Sphere.Services.Service
{
    internal class NearbyService(IApiService apiService) : INearbyService
    {
        private readonly IApiService _apiService = apiService;
        public async Task<ApiResponse<IEnumerable<NearbyModel>>> GetNearbyUsersAsync(NearbyRequest request)
        {
            var url = QueryHelpers.AddQueryString("api/nearby", new Dictionary<string, string?>
            {
                ["Latitude"] = request.Latitude?.ToString(CultureInfo.InvariantCulture),
                ["Longitude"] = request.Longitude?.ToString(CultureInfo.InvariantCulture),
                ["DistanceKm"] = request.DistanceKm.ToString(CultureInfo.InvariantCulture),
                ["Page"] = request.Page.ToString(),
                ["PageSize"] = request.PageSize.ToString(),
                ["Gender"] = request.Gender.HasValue ? ((int)request.Gender.Value).ToString() : null
            });

            // Trả về trực tiếp Task từ _apiService (không cần await nếu không xử lý thêm).
            return await _apiService.GetAsync<IEnumerable<NearbyModel>>(url);
        }

        
    }
}
