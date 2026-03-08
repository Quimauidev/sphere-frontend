using Android.Content;
using Android.Locations;
using Sphere.Common.Constans;
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
        _viewModel.ErrorMessage = "GPS đã bị tắt.";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
        _permissionService.ReturnedFromSettings += OnReturnedFromSettings;
        _permissionService.GpsTurnedOff -= HandleGpsTurnedOff;
        _permissionService.GpsTurnedOff += HandleGpsTurnedOff;

        await _viewModel.InitAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _permissionService.ReturnedFromSettings -= OnReturnedFromSettings;
        _permissionService.GpsTurnedOff -= HandleGpsTurnedOff;
    }

    private async void OnReturnedFromSettings()
    {
        await _viewModel.CheckPermissionAndGps();
    }
}