using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class UserWithUserProfileModel
    {
        public UserModel? UserDTO { get; set; }
        public UserProfileModel? UserProfileDTO { get; set; }
    }
}