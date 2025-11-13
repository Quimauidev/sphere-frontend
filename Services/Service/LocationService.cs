using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class LocationService(IApiService apiService) : ILocationService
    {
        private readonly IApiService _apiService = apiService;

        public async Task<ApiResponse<UserLocationModel>> UpdateLocationAsync(UserLocationModel model)
        {
           return await _apiService.PostAsync<UserLocationModel, UserLocationModel>("api/location", model);
        }
    }
}
