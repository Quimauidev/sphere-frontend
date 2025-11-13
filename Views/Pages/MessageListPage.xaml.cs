using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class MessageListPage : ContentPage
{
	public MessageListPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<ConversationsViewModel>();
    }
}