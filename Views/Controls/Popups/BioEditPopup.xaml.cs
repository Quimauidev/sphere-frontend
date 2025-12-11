using CommunityToolkit.Maui.Views;

namespace Sphere.Views.Controls;

public partial class BioEditPopup : Popup
{
	public BioEditPopup(string? initialBio)
	{
		InitializeComponent();
        CurrentBio = initialBio;
        BindingContext = this;
    }
    public string? CurrentBio { get; set; }

    private void OnSave(object sender, EventArgs e)
    {
        Close(CurrentBio);
    }
}