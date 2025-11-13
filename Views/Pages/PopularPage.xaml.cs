using AndroidX.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class PopularPage : ContentPage
{
    public PopularPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<HomeViewModel>();
    }
}