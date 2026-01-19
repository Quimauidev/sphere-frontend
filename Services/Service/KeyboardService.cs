using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    // ẩn bàn phím
    internal class KeyboardService
    {
        public static void HideKeyboard()
        {
#if ANDROID
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var activity = Platform.CurrentActivity;
                if (activity == null)
                    return;

                var view = activity.CurrentFocus ?? activity.Window!.DecorView;
                var token = view?.WindowToken;

                if (token != null &&
                    activity.GetSystemService(Android.Content.Context.InputMethodService)
                    is Android.Views.InputMethods.InputMethodManager imm)
                {
                    imm.HideSoftInputFromWindow(token, Android.Views.InputMethods.HideSoftInputFlags.None);
                }
            });
#endif
        }
    }

}