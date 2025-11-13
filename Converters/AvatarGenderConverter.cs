using Sphere.Common.Constans;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Converters
{
    public class AvatarGenderConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is UserDiaryModel user)
            {
                return !string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? user.AvatarUrl
                    : user.Gender switch
                    {
                        Gender.Male => "man.png",
                        Gender.Female => "woman.png",
                        _ => "man.png"
                    };
            }

            return "man.png";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
