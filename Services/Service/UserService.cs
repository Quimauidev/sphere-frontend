using Kotlin.Contracts;
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
    class UserService(IApiService apiService) : IUserService
    {
        public async Task<ApiResponse<UserModel>> GetUserAsync()
        {
            return await apiService.GetAsync<UserModel>("api/user");
        }

        public async Task<ApiResponse<string>> PhoneExist(string phone)
        {
           return await apiService.GetAsync<string>($"api/user/phone-exist?phone={phone}");
        }

        public async Task<ApiResponse<UserModel>> UpdateProfileAsync(Guid id, List<JsonPatchOperation> patchData)
        {
            return await apiService.PatchAsync<List<JsonPatchOperation>, UserModel>($"api/user/{id}", patchData);
        }
    }
}
