using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Controls.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    partial class DiamondViewModel : ObservableObject
    {
        private readonly IDiamondsService _diamondsService;
        private readonly IUserSessionService _userSessionService;
        [ObservableProperty]
        private ObservableCollection<DiamondModel> packages = new();
        [ObservableProperty]
        private bool isLoading;
        [ObservableProperty]
        private long coins;
        public DiamondViewModel(IDiamondsService diamondsService, IUserSessionService userSessionService)
        {
            _diamondsService= diamondsService;
            _userSessionService = userSessionService;
            Coins = _userSessionService.CurrentUser!.UserProfileDTO!.Coins;
            _ = LoadPackagesAsync();
        }
        
        public async Task LoadPackagesAsync()
        {
            if(IsLoading) return;
            IsLoading = true;
            try
            {
                var response = await _diamondsService.GetUserDiamondsAsync();
                if (response.IsSuccess && response.Data != null)
                {
                    Packages = new ObservableCollection<DiamondModel>(response.Data);
                }
            }
           
            finally
            {
                IsLoading = false;  
            }
        }

        [RelayCommand]
        public async Task SelectPackage(DiamondModel package)
        {
            // QR của admin – bạn tự đổi đường dẫn ảnh
            string qrImagePath = "vietcombank.jpg";

            var popup = new RechargePopup(package, qrImagePath, _userSessionService);
            // Giống y như popup edit bio
            var result = await Application.Current!.MainPage!.ShowPopupAsync(popup);

            
        }

    }
}
