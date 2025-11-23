using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IUserSessionService
    {
        UserWithUserProfileModel? CurrentUser { get; set; }

    }
}