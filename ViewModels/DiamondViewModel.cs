using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Models;
using Sphere.Services.IService;
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
        private readonly IUserSessionService _userSession;
        [ObservableProperty]
        private ObservableCollection<DiamondModel> packages = new();
        [ObservableProperty]
        private bool isLoading;
        [ObservableProperty]
        private long coins;
        public DiamondViewModel(IDiamondsService diamondsService, IUserSessionService userSession)
        {
            _diamondsService= diamondsService;
            _userSession = userSession;
            Coins = _userSession.CurrentUser!.UserProfileDTO!.Coins;
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
    }
}
