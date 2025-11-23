using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class UserModel
    {
        public Guid Id { get; set; } // ID người dùng
        public long UserIdNumber { get; set; } // ID định danh người dùng 16 số
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? BirthDay { get; set; }
        public DateTime CreatedAt { get; set; }

        // Phương thức chuyển đổi ngày sinh thành định dạng ISO 8601 (hoặc kiểu khác)
        public string GetFormattedBirthDateUtc() =>
        BirthDay.HasValue
         ? BirthDay.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
         : string.Empty;
    }
}