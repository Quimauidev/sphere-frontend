using CommunityToolkit.Maui.Views;

namespace Sphere.Views.Controls;

public partial class BottomSheetPopup : Popup
{
	public BottomSheetPopup()
	{
		InitializeComponent();
        SheetContent.TranslateToAsync(0, 0, 300, Easing.SinOut);
    }
}