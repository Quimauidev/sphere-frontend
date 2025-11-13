using Sphere.Common.Responses;
using Sphere.Models;
using System;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IFollowService
    {
        Task<ApiResponse<UserDiaryModel>> FollowUserAsync(Guid userId);
        Task<ApiResponse<UserDiaryModel>> UnfollowUserAsync(Guid userId);
    }
}
