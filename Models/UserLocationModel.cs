using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class UserLocationModel
    {
        public double? Latitude { get; set; } // vĩ độ
        public double? Longitude { get; set; } // kinh độ
        public DateTime LastUpdated { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
