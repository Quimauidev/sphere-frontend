using Sphere.Common.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    internal interface IRefreshTokenService
    {
        Task<ApiResponse<bool>> TryRefreshTokenAsync();
    }
}
