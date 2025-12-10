using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Responses;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class ForgetPasswordViewModel() : ObservableObject
    {
        //private readonly IUserService _userService; 
        [ObservableProperty]
        public partial bool isLoading { get; set; }
        [ObservableProperty]
        public string Phone { get; set; } = string.Empty;

        //[RelayCommand]
        //public async Task SendOtpAsync()
        //{
        //    if(string.IsNullOrWhiteSpace(Phone))
        //    {
        //        await ApiResponseHelper.ShowAlertAsync("Vui lòng nhập số điện thoại.");
        //        return;
        //    }
        //    var phone = _userService.
        //}
    }
}
