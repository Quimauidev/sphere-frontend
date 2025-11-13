using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Extensions
{
    public static class AppPermissionExtensions
    {
        public static string ToFriendlyName(this AppPermission permission)
        {
            return permission switch
            {
                AppPermission.ReadImages => "bộ sưu tập",
                AppPermission.Camera => "máy ảnh",
                AppPermission.Location => "vị trí",
                AppPermission.Microphone => "microphone",
                _ => "quyền không xác định"
            };
        }
    }

}
