using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class GenderToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // So sánh trực tiếp enum Gender với Gender
            return value is Gender gender && parameter is Gender genderParam && gender == genderParam;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Nếu radio được check và parameter là Gender enum, thì trả về Gender
            return (value is bool isChecked && isChecked && parameter is Gender genderParam)
                ? genderParam
                : Binding.DoNothing;
        }
    }

}
