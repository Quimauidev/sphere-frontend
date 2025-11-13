using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ImagePickerPage : ContentPage
{
    public ImagePickerPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<ImagePickerViewModel>();
    }

}