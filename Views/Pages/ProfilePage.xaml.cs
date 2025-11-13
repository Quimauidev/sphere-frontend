
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<ProfileViewModel>();
       
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ProfileViewModel)?.RefreshFromSession();
    }
}