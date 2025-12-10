using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class DiamondPage : ContentPage
{
	public DiamondPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		BindingContext = serviceProvider.GetRequiredService<DiamondViewModel>();
    }
}