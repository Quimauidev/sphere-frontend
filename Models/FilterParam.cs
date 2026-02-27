using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Models
{
    public class FilterParam
    {
        public Action<Gender?, int, bool>? OnApply { get; set; }
    }
}
