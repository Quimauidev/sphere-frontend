using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class FilterPage : ContentPage
{
	public FilterPage(FilterPageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}