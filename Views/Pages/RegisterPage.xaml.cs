using Sphere.ViewModels;
using System.Globalization;

namespace Sphere.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<RegisterViewModel>();
	}  
}