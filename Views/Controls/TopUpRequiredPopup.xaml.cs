using CommunityToolkit.Maui.Views;

namespace Sphere.Views.Controls;

public partial class TopUpRequiredPopup : Popup
{
    public TopUpRequiredPopup()
    {
        InitializeComponent();
    }

    public event EventHandler? TopUpRequested;

    private void OnClose(object sender, EventArgs e)
    {
       
        Close(false); // false: không nạp kim cương
    }

    private void OnTopUp(object sender, EventArgs e)
    {
        TopUpRequested?.Invoke(this, EventArgs.Empty);
        Close(true); // true: nạp kim cương
    }
}
