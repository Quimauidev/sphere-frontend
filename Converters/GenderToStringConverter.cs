using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sphere.Converters
{
    public class GenderToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                Gender.Male => "Nam",
                Gender.Female => "Nữ",
                null => "Tất cả",
                _ => "Tất cả"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                "Nam" => Gender.Male,
                "Nữ" => Gender.Female,
                "Tất cả" => null,
                _ => null
            };
        }
    }
}
