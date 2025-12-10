using Sphere.Common.Responses;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IDiamondsService
    {
        Task<ApiResponse<IEnumerable<DiamondModel>>> GetUserDiamondsAsync();
    }
}
