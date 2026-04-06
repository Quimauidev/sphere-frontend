using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.DTOs
{
    public class NearbyRequest
    {
        public double? Latitude { get; set; } // Kinh độ
        public double? Longitude { get; set; } // Vĩ độ
        public int DistanceKm { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public Gender? Gender { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }
}
