using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Models.AuthModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IUserProfileService
    {
        Task<ApiResponse<UserWithUserProfileModel>> GetUserProfileMeAsync();
        Task<ApiResponse<UserWithUserProfileModel>> GetUserProfileOtherAsync(Guid userId);
        Task<ApiResponse<UserProfileModel>> UpdateBioAsync(Guid id, BioProfileModel bio);

        Task<ApiResponse<UserProfileModel>> UpdateAvatarAsync(Guid id, string avatarUrl);

        Task<ApiResponse<UserProfileModel>> UpdateCoverPhotoAsync(Guid id, string coverPhotoUrl);
        Task<ApiResponse<UserProfileModel>> DeleteAvatarAsync(Guid id);
        Task<ApiResponse<UserProfileModel>> DeleteCoverPhotoAsync(Guid id);
    }
}