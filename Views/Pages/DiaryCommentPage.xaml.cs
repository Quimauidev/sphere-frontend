using AndroidX.Lifecycle;
using Sphere.ViewModels.DiaryViewModels;
using Microsoft.Maui.Controls;
using Sphere.Models;

namespace Sphere.Views.Pages;

public partial class DiaryCommentPage : ContentPage
{
    private readonly DiaryCommentViewModel _vm;
    public DiaryCommentPage(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _vm = serviceProvider.GetRequiredService<DiaryCommentViewModel>();
        BindingContext = _vm;
        _vm.RequestFocusCommentEditor = async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(100); // cho layout ổn định
                CommentEntry.Focus();
            });
        };

        _vm.ScrollToFlatItem = item =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(400); // đợi keyboard
                CommentsCollection.ScrollTo(
                    item,
                    position: ScrollToPosition.MakeVisible,
                    animate: true);
            });
        };

    }
}