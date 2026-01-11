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
        Task<ApiResponse<IEnumerable<DiaryModel>>> GetListDiaryAsync(int page, int pageSize);
        Task<ApiResponse<DiaryModel>> PatchFormDiaryByIdAsync(Guid id, string? content, Privacy privacy, IEnumerable<Guid> removeImageIds, IEnumerable<string> newImagePaths);
        Task<ApiResponse<bool>> DeleteDiaryAsync(Guid id);
        Task<ApiResponse<IEnumerable<UserWithDiaryModel>>> GetHomeDiariesAsync(string type, int page, int pageSize);
        Task<ApiResponse<DiaryLikeStatusDTO>> SetLikeAsync(Guid diaryId);
    }
}
