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
            // Nếu là "All", checked khi SelectedGender == null
            if (parameter is string str && str == "All")
                return value == null;

            // Nếu là Gender enum, checked khi value == genderParam
            if (value is Gender gender && parameter is Gender genderParam)
                return gender == genderParam;

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                if (parameter is string str && str == "All") return null; // chọn "Tất cả"
                if (parameter is Gender genderParam) return genderParam; // chọn Nam/Nữ
            }

            return Binding.DoNothing;
        }
    }

}
