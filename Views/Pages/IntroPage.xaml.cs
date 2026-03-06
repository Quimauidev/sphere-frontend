using Sphere.Common.Helpers;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class IntroPage : ContentPage
{
    public Func<Task>? OnFinishedIntro;
    public IntroPage()
	{
		InitializeComponent();
	}

    private async void OnGetStartedClicked(object sender, EventArgs e)
    {
        // Lưu trạng thái đã xem intro
        PreferencesHelper.SetIntroShown();
        // Lấy layout chính của IntroPage
        var layout = this.Content;

        if (layout != null)
        {
            await Task.WhenAll(
                layout.FadeToAsync(0, 500),
                layout.TranslateToAsync(0, -layout.Height, 500),
                layout.ScaleToAsync(0.95, 500));

        }
        // Chuyển đến HomePage
        if (OnFinishedIntro != null)
            await OnFinishedIntro();
    }
}