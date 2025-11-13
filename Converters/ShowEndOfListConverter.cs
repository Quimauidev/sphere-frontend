using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class ShowEndOfListConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3 ||
            values[0] is not bool hasNoMoreData ||
            values[1] is not bool isDiaryLoading ||
            values[2] is not bool hasAnyData)
                return false;
            return hasNoMoreData && !isDiaryLoading && hasAnyData;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
