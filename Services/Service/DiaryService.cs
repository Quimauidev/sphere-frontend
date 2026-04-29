using Microsoft.Maui;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class DiaryService(IApiService apiService) : IDiaryService
    {
        public Task<ApiResponse<DiaryCommentUIModel>> CreateCommentAsync( Guid diaryId, string content, Guid? replyToCommentId = null)
        {
            var request = new PostDiaryCommentModel
            {
                Content = content,
                ParentCommentId = replyToCommentId
            };

            return apiService.PostAsync< PostDiaryCommentModel, DiaryCommentUIModel >($"api/diary/{diaryId}/comments", request);
        }


        public async Task<ApiResponse<DiaryModel>> CreateDiaryAsync(PostDiaryModel postStatusModel)
        {
            // Prepare multipart form data
            var form = new MultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(postStatusModel.Content))
                form.Add(new StringContent(postStatusModel.Content), "Content");

            // Add privacy as string
            form.Add(new StringContent(postStatusModel.Privacy.ToString()), "Privacy");

            // Add images if any
            if (postStatusModel.ImagePaths != null)
            {
                foreach (var path in postStatusModel.ImagePaths)
                {
                    var fileName = Path.GetFileName(path);
                    var stream = File.OpenRead(path);
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    form.Add(fileContent, $"Images", fileName);
                }
            }
            return await apiService.PostFormAsync<DiaryModel>("api/diary", form);
        }

        public async Task<ApiResponse<bool>> DeleteCommentAsync(Guid commentId)
        {
            return await apiService.DeleteAsync<bool>($"api/diary/comments/{commentId}");
        }

        public async Task<ApiResponse<bool>> DeleteDiaryAsync(Guid id)
        {
            return await apiService.DeleteAsync<bool>($"api/diary/{id}");
        }

        public async Task<ApiResponse<IEnumerable<DiaryCommentUIModel>>> GetCommentAsync(Guid id, int page, int pageSize)
        {
            return await apiService.GetAsync<IEnumerable<DiaryCommentUIModel>>($"api/diary/{id}/comments?page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<IEnumerable<UserWithDiaryModel>>> GetHomeDiariesAsync(string type, int page, int pageSize, CancellationToken ct = default)
        {
            return await apiService.GetAsync<IEnumerable<UserWithDiaryModel>>($"api/diary/home?type={type}&page={page}&pageSize={pageSize}", ct);
        }

        public async Task<ApiResponse<IEnumerable<DiaryModel>>> GetListDiaryMeAsync(int page, int pageSize)
        {
            return await apiService.GetAsync<IEnumerable<DiaryModel>>($"api/diary/me?page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<IEnumerable<DiaryModel>>> GetListDiaryOtherAsync(Guid id, int page, int pageSize)
        {
            return await apiService.GetAsync<IEnumerable<DiaryModel>>($"api/diary/user/{id}?page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<IEnumerable<DiaryCommentUIModel>>> GetRepliesAsync(Guid id, int page, int pageSize)
        {
            return await apiService.GetAsync<IEnumerable<DiaryCommentUIModel>>($"api/diary/comments/{id}/replies?page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<DiaryModel>> PatchFormDiaryByIdAsync(Guid id, string? content, Privacy privacy, IEnumerable<Guid> removeImageIds, IEnumerable<string> newImagePaths)
        {
            var formData = new MultipartFormDataContent();

            // Add content only if it is not null
            if (!string.IsNullOrEmpty(content))
            {
                formData.Add(new StringContent(content), "Content");
            }

            // Convert Privacy enum to string before adding
            formData.Add(new StringContent(privacy.ToString()), "Privacy");

            foreach (var imageId in removeImageIds)
            {
                formData.Add(new StringContent(imageId.ToString()), "RemoveImageIds");
            }
            // Add images (if any)
            foreach (var imagePath in newImagePaths)
            {
                try
                {
                    if (!File.Exists(imagePath)) continue;
                    var fileName = Path.GetFileName(imagePath);
                    var stream = File.OpenRead(imagePath);
                    var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // or appropriate MIME type

                    formData.Add(fileContent, "NewImages", fileName);
                }
                catch
                {
                    // Ignore file errors
                }
            }

            return await apiService.PatchFormAsync<DiaryModel>($"api/diary/{id}", formData);
        }

        public async Task<ApiResponse<bool>> ReportCommentAsync(Guid commentId)
        {
          return await apiService.PostAsync<object, bool>($"api/diary/comment/{commentId}/report", null!);
        }

        public async Task<ApiResponse<CommentLikeStatusDTO>> SetCommentLikeAsync(Guid commentId)
        {
            return await apiService.PostAsync<object, CommentLikeStatusDTO>($"api/diary/comment/{commentId}/like", null!);
        }

        //public async Task<ApiResponse<DiaryLikeStatusDTO>> SetLikeAsync(Guid diaryId)
        //{
        //    return await apiService.PostAsync<object, DiaryLikeStatusDTO>( $"api/diary/{diaryId}/like",null!);
        //}
        public async Task<ApiResponse<DiaryLikeStatusDTO>> SetLikeAsync(Guid diaryId, bool isLiked)
        {
            return await apiService.PutAsync<object, DiaryLikeStatusDTO>( $"api/diary/like", new { diaryId, isLiked });
        }
        public async Task<ApiResponse<DiaryCommentUIModel>> UpdateCommentAsync(Guid commentId, string? newContent, Guid? replyToUserId)
        {
            var body = new 
            {
                Content = newContent?.Trim(),
                ReplyToUserProfileId = replyToUserId // null ⇒ comment gốc
            };
            return await apiService.PatchAsync<object, DiaryCommentUIModel>($"api/diary/comments/{commentId}", body);
        }
    }
}