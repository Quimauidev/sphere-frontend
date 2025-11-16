using Sphere.Common.Constans;
using Sphere.ViewModels;

namespace Sphere.Views.Pages;

public partial class MessagePage : ContentPage
{
    private KeyboardListener? _keyboardListener;

    public MessagePage(MessageViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        // Auto scroll xuống cuối
        // Scroll xuống cuối khi có tin nhắn
        vm.ScrollToLastMessage = () =>
        {
            if (vm.Messages.Count > 0)
            {
                this.Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        MessagesList.ScrollTo(vm.Messages.Last(), position: ScrollToPosition.End, animate: true);
                    }
                    catch { }
                });
            }
        };

        // Setup keyboard listener
        var rootView = Platform.CurrentActivity?.Window?.DecorView.RootView;
        if (rootView != null)
        {
            _keyboardListener = new KeyboardListener(heightPx =>
            {
                this.Dispatcher.Dispatch(() =>
                {
                    float density = Platform.CurrentActivity?.Resources?.DisplayMetrics?.Density ?? 1f;
                    double height = density > 0 ? heightPx / density : heightPx;

                    if (height > 0)
                    {
                        InputGrid.TranslationY = -5;

                        // 🔹 Thêm margin dưới để không bị che tin nhắn
                        MessagesList.Margin = new Thickness(10, 5, 10, 10);

                        if (vm.Messages.Count > 0)
                            vm.ScrollToLastMessage?.Invoke();
                    }
                    else
                    {
                        InputGrid.TranslationY = 0;
                        MessagesList.Margin = new Thickness(10, 5, 10, 10);
                    }
                });
            });

            rootView.ViewTreeObserver?.AddOnGlobalLayoutListener(_keyboardListener);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        var rootView = Platform.CurrentActivity?.Window?.DecorView.RootView;
        if (rootView != null && _keyboardListener != null)
            rootView.ViewTreeObserver?.RemoveOnGlobalLayoutListener(_keyboardListener);
    }

}