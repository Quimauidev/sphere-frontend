using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Constans
{
    public class KeyboardListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private readonly Action<int> _onHeightChanged;
        public KeyboardListener(Action<int> onHeightChanged) => _onHeightChanged = onHeightChanged;

        public void OnGlobalLayout()
        {
            var rootView = Platform.CurrentActivity?.Window?.DecorView.RootView;
            if (rootView == null) return;
            var rect = new Android.Graphics.Rect();
            rootView.GetWindowVisibleDisplayFrame(rect);
            var screenHeight = rootView.Height;
            var keypadHeight = screenHeight - rect.Bottom;

            _onHeightChanged?.Invoke(keypadHeight > 150 ? keypadHeight : 0);
        }
    }

}
