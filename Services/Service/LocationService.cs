using Sphere.Common.Responses;
using Sphere.DTOs;
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

        public async Task<ApiResponse<CreateLocationRequest>> CreateLocationAsync(CreateLocationRequest request)
        {
            return await _apiService.PostAsync<CreateLocationRequest, CreateLocationRequest>("api/nearby", request);
        }

        public async Task<ApiResponse<object>> SetLocationVisibilityAsync(bool isVisible)
        {
            return await _apiService.PutAsync<object, object>("api/nearby/visibility", isVisible);
        }

        //public async Task<ApiResponse<CreateUpdateLocationRequest>> UpdateLocationAsync(CreateUpdateLocationRequest request)
        //{
        //    return await _apiService.PutAsync<CreateUpdateLocationRequest, CreateUpdateLocationRequest>("api/nearby/update-location", request);
        //}
    }
}
