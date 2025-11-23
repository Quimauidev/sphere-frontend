using Android.Content;
using Android.Locations;
using Sphere.Services.IService;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class NearbyPage : ContentPage
{
    private readonly NearbyViewModel _viewModel;
    private readonly IPermissionService _permissionService;
    public NearbyPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        _viewModel = serviceProvider.GetRequiredService<NearbyViewModel>();
        BindingContext = _viewModel;
        _permissionService = serviceProvider.GetRequiredService<IPermissionService>();
    }
    private void HandleGpsTurnedOff()
    {
        if (_viewModel.IsLocationEnabled)
            _viewModel.IsLocationEnabled = false;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
        _permissionService.ReturnedFromSettings += OnReturnedFromSettings;
        // 🔹 Đăng ký event tắt GPS ngoài app
        _permissionService.GpsTurnedOff -= HandleGpsTurnedOff;
        _permissionService.GpsTurnedOff += HandleGpsTurnedOff;
        // Chỉ gọi InitAsync nếu chưa có dữ liệu Nearby
        if (_viewModel.Nearby == null || _viewModel.Nearby.Count == 0)
        {
            await _viewModel.InitAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
        // 🔹 Hủy đăng ký event khi page biến mất
        _permissionService.GpsTurnedOff -= HandleGpsTurnedOff;
    }

    private async void OnReturnedFromSettings()
    {
            await _viewModel.CheckGpsAfterSettingsAsync();
    }

}