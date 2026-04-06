using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.DTOs
{
    public class FilterService
    {
        public Gender? SelectedGender { get; set; } = null;
        public int MinAge { get; set; } = 16;
        public int MaxAge { get; set; } = 80;
        public int Distance { get; set; } = 10; // km

        public void Reset()
        {
            SelectedGender = null;
            MinAge = 16;
            MaxAge = 80;
            Distance = 10;
        }
    }
}
