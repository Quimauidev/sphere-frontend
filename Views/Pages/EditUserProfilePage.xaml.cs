using Microsoft.Extensions.DependencyInjection;
using Sphere.ViewModels;
using System.Globalization;

namespace Sphere.Views.Pages;

public partial class EditUserProfilePage : ContentPage
{
    public EditUserProfilePage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<UserViewModel>();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Entry? entry = sender as Entry;
        entry?.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entry.Text.ToLower());
    }
}