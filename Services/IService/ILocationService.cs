using Sphere.Common.Responses;
using Sphere.DTOs;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface ILocationService
    {
        //Task<ApiResponse<CreateUpdateLocationRequest>> UpdateLocationAsync(CreateUpdateLocationRequest model);
        Task<ApiResponse<CreateLocationRequest>> CreateLocationAsync(CreateLocationRequest request);
        Task<ApiResponse<object>> SetLocationVisibilityAsync(bool isVisible);
    }
}
