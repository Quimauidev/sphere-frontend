using Microsoft.Extensions.DependencyInjection;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class LoginPage : ContentPage
{

    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}