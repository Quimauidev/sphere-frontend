using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class NearbyModel
    {
        public Guid UserId { get; set; }
        public string? AvatarUrl { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; } // tiểu sử ngắn
        public int Age { get; set; }
        public Gender Gender { get; set; }

        public string? DistanceDisplay { get; set; } // VD: "534m" hoặc "4km"
        public bool IsFollowed { get; set; } // theo dỗi
        public bool CanChat { get; set; } // mở khóa chát

        public string AvatarDisplay => !string.IsNullOrEmpty(AvatarUrl) ? AvatarUrl : Gender == Gender.Male ? "man.png" : "woman.png";
    }
}
