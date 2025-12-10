using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		BindingContext = serviceProvider.GetRequiredService<ForgetPasswordViewModel>();
    }
}