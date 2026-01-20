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
                return string.Empty;

            var timeVN = time.ToVietnamTime();
            var now = DateTime.UtcNow.ToVietnamTime();
            var diff = now - timeVN;

            if (diff.TotalSeconds < 60)
                return "Vừa đăng";

            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} phút trước";

            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} giờ trước";

            if (timeVN.Date == now.Date.AddDays(-1))
                return "Hôm qua";

            if (diff.TotalDays < 14)
                return $"{(int)diff.TotalDays} ngày trước";

            if (diff.TotalDays < 60)
                return $"{Math.Max(1, (int)(diff.TotalDays / 7))} tuần trước";

            if (diff.TotalDays < 365)
                return $"{(int)(diff.TotalDays / 30)} tháng trước";

            return $"{(int)(diff.TotalDays / 365)} năm trước";

        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
