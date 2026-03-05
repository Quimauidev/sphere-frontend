using Sphere.Common.Responses;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IUserService
    {
        Task<ApiResponse<UserModel>> GetUserAsync();
        Task<ApiResponse<UserModel>> UpdateProfileAsync(Guid id, List<JsonPatchOperation> patchData);
        Task<ApiResponse<string>> PhoneExist(string phone);
    }
}
