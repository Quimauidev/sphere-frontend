using Android.Media.TV;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Models.AuthModel;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class AuthService(IApiService apiService) : IAuthService
    {
       
        public Task<ApiResponse<AuthResponse>> LoginAsync(LoginModel model)
        => apiService.SendRequestAsync<LoginModel, AuthResponse>(HttpMethod.Post,"/api/auth/login", model, requireAuth: false);

        public Task<ApiResponse<UserModel>> RegisterAsync(RegisterModel model)
            => apiService.SendRequestAsync <RegisterModel, UserModel>(HttpMethod.Post,"/api/auth/register", model, requireAuth: false);
        public async Task<ApiResponse<bool>> LogoutAsync()
        {
            var refreshTokenId = PreferencesHelper.GetRefreshTokenId();
            if (string.IsNullOrEmpty(refreshTokenId))
            {
                return ApiResponse<bool>.Fail("Không có refresh token", []);
            }

            var response = await apiService.DeleteAsync<bool>($"/api/auth/logout/{refreshTokenId}");

            if (response.IsSuccess)
            {
                PreferencesHelper.Logout(); // Xoá local token luôn
            }

            return response;
        }
    }
}