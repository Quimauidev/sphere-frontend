using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class FollowService(IApiService apiService) : IFollowService
    {
        public async Task<ApiResponse<UserDiaryModel>> FollowUserAsync(Guid userId)
        {
            // Gửi yêu cầu follow
            return await apiService.PostAsync<object, UserDiaryModel>($"api/follows/{userId}", new object());
        }

        public async Task<ApiResponse<UserDiaryModel>> UnfollowUserAsync(Guid userId)
        {
            // Gửi yêu cầu unfollow
            return await apiService.DeleteAsync<UserDiaryModel>($"api/follows/{userId}");
        }
    }
}
