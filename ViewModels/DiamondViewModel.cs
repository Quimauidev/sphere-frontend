using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
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
    public partial class DiamondViewModel : ObservableObject
    {
        private readonly IDiamondsService _diamondsService;
        private readonly IUserSessionService _userSessionService;
        private readonly IShellNavigationService _nv;
        private readonly ApiResponseHelper _res;

        [ObservableProperty]
        private ObservableCollection<DiamondModel> packages = [];

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private long coins;

        public DiamondViewModel(IDiamondsService diamondsService, IUserSessionService userSessionService, IShellNavigationService nv, ApiResponseHelper res)
        {
            _diamondsService = diamondsService;
            _userSessionService = userSessionService;
            _nv = nv;
            _res = res;
            Coins = _userSessionService.CurrentUser!.UserProfileDTO!.Coins;
            _ = LoadPackagesAsync();
            
        }

        public async Task LoadPackagesAsync(bool forceRefresh = false, bool showLoading = true)
        {
            if (IsLoading) return;
            IsLoading = true;
            if (showLoading)
                await PopupHelper.ShowLoadingAsync();
            try
            {
                // Lấy từ cache trước
                if (!forceRefresh)
                {
                    var cached = PreferencesHelper.LoadDiamondPackages();
                    if (cached != null)
                    {
                        Packages = new ObservableCollection<DiamondModel>(cached);
                        return;
                    }
                }

                // Gọi API nếu chưa cache hoặc forceRefresh
                var response = await _diamondsService.GetUserDiamondsAsync();
                if (response.IsSuccess && response.Data != null)
                {
                    Packages = new ObservableCollection<DiamondModel>(response.Data);

                    // Lưu vào Preferences
                    PreferencesHelper.SaveDiamondPackages(response.Data);
                }
                else
                    await _res.ShowApiErrorsAsync(response);
            }
            finally
            {
                if (showLoading)
                    await PopupHelper.HideLoadingAsync();
                IsLoading = false;
                IsRefreshing = false; // đảm bảo refresh view tắt sau khi load xong
            }
        }

        [RelayCommand]
        public async Task SelectPackage(DiamondModel package)
        {
            // QR của admin – bạn tự đổi đường dẫn ảnh
            string qrImagePath = "vietcombank.jpg";

            var popup = new RechargePopup(package, qrImagePath, _userSessionService);
            // Giống y như popup edit bio
            _ = await _nv.ShowPopupAsync(popup);
        }

        [RelayCommand]
        private async Task RefreshPackagesAsync()
        {
            try
            {
                IsRefreshing = true;
                // Xóa cache gói nạp
                PreferencesHelper.ClearDiamondPackages();

                // Tải lại từ API
                await LoadPackagesAsync(forceRefresh: true, showLoading: false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }
    }
}