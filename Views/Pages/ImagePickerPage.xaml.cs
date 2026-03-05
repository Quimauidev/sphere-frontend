using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class ImagePickerPage : ContentPage
{
    public ImagePickerPage(ImagePickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

}