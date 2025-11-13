using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class ShowNoDataMessageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return false;

            int count = values[0] as int? ?? 0;
            bool isLoading = values[1] as bool? ?? false;
            string? error = values[2] as string;

            return count == 0 && !isLoading && string.IsNullOrEmpty(error);

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
