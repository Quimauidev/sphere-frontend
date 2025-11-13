using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static Android.Graphics.ColorSpace;

namespace Sphere.Services.Service
{
    internal class UserProfileMeService(IApiService apiService) : IUserProfileService
    {
        public async Task<ApiResponse<UserProfileModel>> DeleteAvatarAsync(Guid id)
        {
            return await apiService.DeleteAsync<UserProfileModel>($"api/profile/avatar/{id}");
        }

        public async Task<ApiResponse<UserProfileModel>> DeleteCoverPhotoAsync(Guid id)
        {
            return await apiService.DeleteAsync<UserProfileModel>($"api/profile/photo/{id}");
        }

        public async Task<ApiResponse<UserWithUserProfileModel>> GetUserProfileMeAsync()
        {
            return await apiService.GetAsync<UserWithUserProfileModel>("api/profile/me");
        }

        public async Task<ApiResponse<UserProfileModel>> UpdateAvatarAsync(Guid id, string avatarUrl)
        {
            var form = new MultipartFormDataContent();

            var fileName = Path.GetFileName(avatarUrl);
            var stream = File.OpenRead(avatarUrl);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // or appropriate MIME type

            form.Add(fileContent, "AvatarImage", fileName);

            return await apiService.PutFormAsync<UserProfileModel>($"api/profile/avatar/{id}", form);
        }

        public async Task<ApiResponse<UserProfileModel>> UpdateBioAsync(Guid id, BioProfileModel bio)
        {
            return await apiService.PutAsync<BioProfileModel, UserProfileModel>($"api/profile/bio/{id}", bio);
        }

        public async Task<ApiResponse<UserProfileModel>> UpdateCoverPhotoAsync(Guid id, string coverPhotoUrl)
        {
            var form = new MultipartFormDataContent();

            var fileName = Path.GetFileName(coverPhotoUrl);
            var stream = File.OpenRead(coverPhotoUrl);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // or appropriate MIME type

            form.Add(fileContent, "CoverPhotoImage", fileName);

            return await apiService.PutFormAsync<UserProfileModel>($"api/profile/cover-photo/{id}", form);
        }
    }
}