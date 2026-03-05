using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Helpers
{
    internal class PrivacyValues
    {
        public static List<Privacy> All { get; } = [.. Enum.GetValues<Privacy>().Cast<Privacy>()];
    }
}
