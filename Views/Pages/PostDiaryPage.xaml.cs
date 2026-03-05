using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class PostDiaryPage : ContentPage
{
    public PostDiaryPage(PostDiaryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        GalleryView.ItemTemplate = new GalleryItemTemplateSelector
        {
            ImageTemplate = (DataTemplate)Resources["ImageTemplate"],
            AddButtonTemplate = (DataTemplate)Resources["AddButtonTemplate"]
        };
    }
}