using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Models
{
    public class FilterParam
    {
        public Action<Gender?, int, int, int>? OnApply { get; set; }
        // Các giá trị filter hiện tại để FilterPage hiển thị
        public Gender? SelectedGender { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public int Distance { get; set; }
    }
}
