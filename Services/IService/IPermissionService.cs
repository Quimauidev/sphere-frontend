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
        Task<bool> EnsureGrantedAsync(AppPermission permission);
        Task<bool> CheckGpsStatusAsync();

        // 👇 Thêm dòng này
        event Action? ReturnedFromSettings;
        bool IsGpsEnabled();
        // 🔹 Thêm event
        event Action? GpsTurnedOff;
    }
}