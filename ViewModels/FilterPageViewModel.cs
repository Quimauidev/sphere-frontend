using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class FilterPageViewModel : ObservableObject
    {
        // callback này được gán từ NearbyViewModel
        public Action<Gender?, int, bool>? OnApply { get; set; }

        [ObservableProperty] private Gender? selectedGender;
        [ObservableProperty] private int distance = 1; // default 10km
        [ObservableProperty] private bool isLocationEnabled;
        public Array GenderOptions => Enum.GetValues(typeof(Gender));


        // Command cho nút Áp dụng
        [RelayCommand]
        private async Task ApplyAsync()
        {
            // gọi callback về NearbyViewModel
            OnApply?.Invoke(SelectedGender, Distance, IsLocationEnabled);

            // đóng trang Filter sau khi áp dụng
            await Shell.Current.Navigation.PopModalAsync();
        }

        // Command cho nút Hủy (nếu cần)
        [RelayCommand]
        private async Task CancelAsync()
        {
            await Shell.Current.Navigation.PopModalAsync();
        }
    }
}
