
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
       
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as ProfileViewModel;

        if (vm?.IsViewingSelf == true)
        {
            vm.RefreshFromSession();
        }
    }
}