using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage(ForgetPasswordViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}