using Android.Content;
using Android.Content.Res;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(Shell), typeof(Sphere.Platforms.Android.Handlers.CustomShellRenderer))]
namespace Sphere.Platforms.Android.Handlers
{
    public class CustomShellRenderer : ShellRenderer
    {
        public CustomShellRenderer(Context context) : base(context) { }

        protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        {
            return new CustomBottomNavViewAppearanceTracker(this, shellItem);
        }
    }

    public class CustomBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
    {
        public CustomBottomNavViewAppearanceTracker(IShellContext context, ShellItem item) : base(context, item) { }

        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);

            // Tab thông báo ở index 1 (sửa nếu khác)
            var menuItem = bottomView.Menu.GetItem(1);

            var badge = bottomView.GetOrCreateBadge(menuItem!.ItemId);

            badge.SetVisible(true);
            badge.Number = 7;

            // Gán màu đỏ cho badge
            badge.BackgroundColor = global::Android.Graphics.Color.Red;
        }
    }
}
