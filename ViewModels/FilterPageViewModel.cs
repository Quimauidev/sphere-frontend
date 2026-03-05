using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Interfaces;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class FilterPageViewModel(IShellNavigationService nv) : ObservableObject, IModalParameterReceiver<FilterParam>
    {
        private readonly IShellNavigationService _nv = nv;

        // callback này được gán từ NearbyViewModel
        //public Action<Gender?, int, bool>? OnApply { get; set; }
        private Action<Gender?, int, bool>? _onApply;
        public async Task Receive(FilterParam param)
        {
            _onApply = param.OnApply;
        }

        [ObservableProperty] private Gender? selectedGender;
        [ObservableProperty] private int distance = 1; // default 10km
        [ObservableProperty] private bool isLocationEnabled; // default false
        public Array GenderOptions => Enum.GetValues(typeof(Gender));


        // Command cho nút Áp dụng
        [RelayCommand]
        private async Task ApplyAsync()
        {
            _onApply?.Invoke(SelectedGender, Distance, IsLocationEnabled);
            await _nv.PopModalAsync();
        }

        // Command cho nút Hủy (nếu cần)
        [RelayCommand]
        private async Task CancelAsync()
        {
            await _nv.PopModalAsync();
        }

        
    }
}
