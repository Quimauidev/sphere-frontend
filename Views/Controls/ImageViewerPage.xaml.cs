namespace Sphere.Views.Controls;

public partial class ImageViewerPage : ContentPage
{
    public ImageViewerPage(string imagePath)
    {
        InitializeComponent();
        FullImage.Source = ImageSource.FromFile(imagePath);
    }
}