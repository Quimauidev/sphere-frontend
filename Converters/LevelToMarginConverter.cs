using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Globalization;

namespace Sphere.Converters
{
    public class LevelToMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var level = (int)value!;
            return level == 0
                ? new Thickness(0)
                : new Thickness(50, 0, 0, 0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
