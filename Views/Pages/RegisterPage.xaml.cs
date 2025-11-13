using Sphere.ViewModels;
using System.Globalization;

namespace Sphere.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(IServiceProvider serviceProvider)
	{
		InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<RegisterViewModel>();
	}

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        if (entry != null)
        {
            // Chuyển đổi chữ hoa chữ cái đầu của mỗi từ
            entry.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entry.Text.ToLower());
        }
    }
}