using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.DTOs;
using Sphere.Interfaces;
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
    public partial class FilterPageViewModel(IShellNavigationService nv) : ObservableObject, IModalParameterReceiver<FilterParam>
    {
        private readonly IShellNavigationService _nv = nv;
        private Action<Gender?, int, int, int>? _onApply; // 4 tham số
        [ObservableProperty] private Gender? selectedGender;
        [ObservableProperty] private int minAge;
        [ObservableProperty] private int maxAge;
        [ObservableProperty] private int distance; // default 10km
        [ObservableProperty] private List<int> maxAgeOptions = [];

        public List<int> AgeOptions { get; } = Enumerable.Range(16, 65).ToList(); // 16 → 80
        public async Task Receive(FilterParam param)
        {
            _onApply = param.OnApply;
            // Gán giá trị hiện tại từ NearbyViewModel
            SelectedGender = param.SelectedGender;
            MinAge = param.MinAge;
            MaxAgeOptions = AgeOptions.Where(x => x >= MinAge).ToList();
            MaxAge = Math.Max(param.MaxAge, MinAge);
            Distance = param.Distance;
        }

        partial void OnMinAgeChanged(int value)
        {
            // 🔥 Max chỉ được >= Min
            MaxAgeOptions = AgeOptions.Where(x => x >= value).ToList();

            // Nếu Max hiện tại < Min → auto fix
            if (MaxAge < value)
                MaxAge = value;
        }
        partial void OnMaxAgeChanged(int value)
        {
            // đảm bảo không bao giờ sai (defensive)
            if (value < MinAge)
                MaxAge = MinAge;
        }
        // Command cho nút Áp dụng
        [RelayCommand]
        private async Task ApplyAsync()
        {
            _onApply?.Invoke(SelectedGender, MinAge, MaxAge, Distance);
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
