using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class UserProfileModel
    {
        public Guid Id { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public long Coins { get; set; } // kim cương
    }

    public class BioProfileModel
    {
        public string? Bio { get; set; } // Nội dung tiểu sử
    }
}