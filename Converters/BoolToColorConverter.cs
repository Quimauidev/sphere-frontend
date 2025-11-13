using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isMine = (bool)value!;
            return isMine ? Color.FromArgb("#f6edff") : Color.FromArgb("#f5f5f5");
            //return isMine ? Colors.white : Colors.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
