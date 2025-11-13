using Sphere.Common.Helpers;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class IntroPage : ContentPage
{
    public Action? OnFinishedIntro;
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
            // Slide layout lên trên + fade out
            await Task.WhenAll(
            layout.FadeTo(0, 500),
            layout.TranslateTo(0, -layout.Height, 500),
            layout.ScaleTo(0.95, 500) // co layout 5% khi slide
            );

        }
        // Chuyển đến HomePage
        OnFinishedIntro?.Invoke();
    }
}