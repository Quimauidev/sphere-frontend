using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class DiamondPage : ContentPage
{
	public DiamondPage(DiamondViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}