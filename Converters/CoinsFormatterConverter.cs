using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class CoinsFormatterConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return "0";

            if (long.TryParse(value.ToString(), out var coins))
            {
                if (coins >= 1_000_000_000_000) // 1 nghìn tỷ
                    return $"{Math.Floor(coins / 100_000_000_000d) / 10} T"; 
                if (coins >= 1_000_000_000) // 1 tỷ
                    return $"{Math.Floor(coins / 100_000_000d) / 10} G";
                if (coins >= 1_000_000) // 1 triệu
                    return $"{Math.Floor(coins / 100_000d) / 10} M";

                return coins.ToString();
            }

            return value.ToString() ?? "0";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
