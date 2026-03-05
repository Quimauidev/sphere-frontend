using Sphere.ViewModels;
using System.Globalization;

namespace Sphere.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}  
}