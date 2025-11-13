using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    public partial class UserSessionService : ObservableObject, IUserSessionService
    {
        [ObservableProperty]
        public UserWithUserProfileModel? currentUser;
    }
}