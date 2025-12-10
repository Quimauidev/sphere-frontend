using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class DiamondService(IApiService apiService) : IDiamondsService
    {
        public async Task<ApiResponse<IEnumerable<DiamondModel>>> GetUserDiamondsAsync()
        {
           return await apiService.GetAsync<IEnumerable<DiamondModel>>("api/diamonds");
        }
    }
}
