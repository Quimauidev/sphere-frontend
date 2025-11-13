using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class FollowPage : ContentPage
{
	public FollowPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<HomeViewModel>();
    }
}