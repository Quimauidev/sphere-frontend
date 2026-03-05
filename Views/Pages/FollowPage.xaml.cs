using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class FollowPage : ContentPage
{
	public FollowPage(HomeViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}