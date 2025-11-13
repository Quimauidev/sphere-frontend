using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class PostDiaryPage : ContentPage
{
    public PostDiaryPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        BindingContext = serviceProvider.GetRequiredService<PostDiaryViewModel>();
        GalleryView.ItemTemplate = new GalleryItemTemplateSelector
        {
            ImageTemplate = (DataTemplate)Resources["ImageTemplate"],
            AddButtonTemplate = (DataTemplate)Resources["AddButtonTemplate"]
        };
    }
}