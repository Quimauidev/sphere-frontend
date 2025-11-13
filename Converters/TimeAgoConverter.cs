using Sphere.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class TimeAgoConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime time || time == default)
                return "Chưa cập nhật";

            var timeVN = time.ToVietnamTime();
            var now = DateTime.UtcNow.ToVietnamTime();
            var diff = now - timeVN;


            if (timeVN.Date == now.Date)
            {
                if (diff.TotalMinutes < 1)
                    return "Vừa đăng";
                if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes} phút trước";
                return $"{(int)diff.TotalHours} giờ trước";
            }

            if (timeVN.Date == now.Date.AddDays(-1))
                return "Hôm qua";

            int daysAgo = (int)diff.TotalDays;
            if (daysAgo < 7)
                return $"{daysAgo} ngày trước";

            if (daysAgo < 30)
            {
                int weeks = daysAgo / 7;
                return $"{weeks} tuần trước";
            }

            if (timeVN.Year == now.Year)
                return timeVN.ToString("dd/MM");

            return timeVN.ToString("dd/MM/yyyy");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
