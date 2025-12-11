using CommunityToolkit.Maui.Views;
using Java.Lang;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.ViewModels;

namespace Sphere.Views.Controls.Popups;

public partial class RechargePopup : Popup
{
	public RechargePopup(DiamondModel package, string qrImage, IUserSessionService userSessionService)
	{
		InitializeComponent();
        BindingContext = new RechargePopupViewModel(package, qrImage, userSessionService);
    }
}