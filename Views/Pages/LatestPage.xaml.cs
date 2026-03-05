using Microsoft.Extensions.DependencyInjection;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class LatestPage : ContentPage
{
	public LatestPage(HomeViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}