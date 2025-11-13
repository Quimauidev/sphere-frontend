using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Extensions
{
    public static class DateTimeExtensions // Fix for CS1106: Extension method must be defined in a non-generic static class
    {
        public static DateTime ToVietnamTime(this DateTime dateTime)
        {
            // Nếu là Utc thì cần chuyển sang giờ Việt Nam
            if (dateTime.Kind == DateTimeKind.Utc)
            {
#if ANDROID
                // Android không hỗ trợ TimeZoneInfo ID kiểu Windows, nên dùng AddHours
                return dateTime.AddHours(7);
#else
                // Trên Windows hoặc iOS có thể dùng TimeZoneInfo
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime, vietnamTimeZone);
#endif
            }

            // Nếu là Local hoặc Unspecified thì giữ nguyên (hoặc xử lý thêm nếu cần)
            return dateTime;
        }

        public static string ToVietnamTimeString(this DateTime dateTime)
        {
            var vietnamTime = dateTime.ToVietnamTime();
            return vietnamTime.ToString("HH:mm dd/MM/yyyy");
        }
    }
}