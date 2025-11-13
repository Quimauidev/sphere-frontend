using Microsoft.Extensions.DependencyInjection;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class LoginPage : ContentPage
{

    public LoginPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<LoginViewModel>();
    }

}