using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models.AuthModel
{
    public class RegisterModel
    {
        public string? FullName { get; set; }
        public Gender Gender { get; set; }
        public DateTime BirthDay { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }

        // Phương thức chuyển đổi ngày sinh thành định dạng ISO 8601
        public string GetFormattedBirthDate()
        {
            return BirthDay.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"); // ISO 8601 format
        }
    }
}