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
       
        _vm.ScrollToIndex = async index =>
        {
            // Đợi keyboard hiển thị (RequestFocus gọi trước trong ViewModel)
            await Task.Delay(400);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var items = CommentsCollection.ItemsSource as System.Collections.IEnumerable;
                // an toàn: nếu không có ItemsSource hoặc index không hợp lệ thì không làm gì
                if (items == null) return;

                // tính số phần tử để kiểm tra index
                int count = 0;
                foreach (var _ in items) count++;

                if (index >= 0 && index < count)
                {
                    // Dùng MakeVisible để đảm bảo item không bị che bởi keyboard
                    CommentsCollection.ScrollTo(
                        index,
                        position: ScrollToPosition.MakeVisible,
                        animate: true);
                }
            });
        };

    }


}