using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class MessageListPage : ContentPage
{
	public MessageListPage(ConversationsViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}