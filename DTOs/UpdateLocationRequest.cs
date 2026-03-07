using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.DTOs
{
    public class UpdateLocationRequest
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool? IsVisible { get; set; }
    }

}
