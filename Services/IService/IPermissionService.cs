using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IPermissionService
    {
        Task<PermissionResult> RequestPermissionAsync(AppPermission permission); 
        Task<bool> CheckGpsStatusAsync();
        event Action? ReturnedFromSettings;
        bool IsGpsEnabled();
        event Action? GpsTurnedOff;
    }
}