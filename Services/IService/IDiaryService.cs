using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IDiaryService
    {
        Task<ApiResponse<DiaryModel>> CreateDiaryAsync(PostDiaryModel postDiaryModel);
        Task<ApiResponse<IEnumerable<DiaryModel>>> GetListDiaryMeAsync(int page, int pageSize);
        Task<ApiResponse<IEnumerable<DiaryModel>>> GetListDiaryOtherAsync(Guid id, int page, int pageSize);
        Task<ApiResponse<DiaryModel>> PatchFormDiaryByIdAsync(Guid id, string? content, Privacy privacy, IEnumerable<Guid> removeImageIds, IEnumerable<string> newImagePaths);
        Task<ApiResponse<bool>> DeleteDiaryAsync(Guid id);
        Task<ApiResponse<IEnumerable<UserWithDiaryModel>>> GetHomeDiariesAsync(string type, int page, int pageSize, CancellationToken ct = default);
        Task<ApiResponse<DiaryLikeStatusDTO>> SetLikeAsync(Guid diaryId);
        Task<ApiResponse<CommentLikeStatusDTO>> SetCommentLikeAsync(Guid commentId, bool isLiked);
        Task<ApiResponse<IEnumerable<DiaryCommentUIModel>>> GetCommentAsync(Guid id, int page,int pageSize);
        Task<ApiResponse<IEnumerable<DiaryCommentUIModel>>> GetRepliesAsync(Guid id, int page, int pageSize);
        Task<ApiResponse<DiaryCommentUIModel>> CreateCommentAsync(Guid diaryId, string content, Guid? replyToCommentId = null);
        Task<ApiResponse<bool>> DeleteCommentAsync(Guid commentId);
        Task<ApiResponse<DiaryCommentUIModel>> UpdateCommentAsync(Guid commentId, string newContent, Guid? replyId);
        // tố cáo
        Task<ApiResponse<bool>> ReportCommentAsync(Guid commentId);
    }
}
