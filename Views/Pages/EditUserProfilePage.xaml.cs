using Microsoft.Extensions.DependencyInjection;
using Sphere.ViewModels;
using System.Globalization;

namespace Sphere.Views.Pages;

public partial class EditUserProfilePage : ContentPage
{
    public EditUserProfilePage(UserViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is UserViewModel vm)
            await vm.InitializeAsync();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Entry? entry = sender as Entry;
        entry?.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(entry.Text.ToLower());
    }
}