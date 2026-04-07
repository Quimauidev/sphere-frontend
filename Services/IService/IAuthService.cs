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
    public interface IAuthService
    {
        Task<ApiResponse<TokenResponse>> LoginAsync(LoginModel model, CancellationToken ct = default);

        Task<ApiResponse<UserModel>> RegisterAsync(RegisterModel model, CancellationToken ct = default);

        Task<ApiResponse<bool>> LogoutAsync(); 
    }
}