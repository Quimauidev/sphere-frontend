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
        public int DistanceKm { get; set; } // khoảng cách chọn lọc (1, 5, 15, 60)
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Gender? Gender { get; set; }
    }
}
