using CommunityToolkit.Maui.Views;
using Sphere.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Helpers
{
    public static class PopupHelper
    {
        private static LoadingPopup? _loadingPopup;
        private static bool _isShowing = false;

        public static void ShowLoading()
        {
            if (_isShowing)
                return;

            _isShowing = true;
            _loadingPopup = new LoadingPopup();
            var page = Application.Current?.MainPage ?? Shell.Current;
            if(page!=null)
            _ = page.ShowPopupAsync(_loadingPopup);
        }

        public static void HideLoading()
        {
            if (_loadingPopup != null)
            {
                _loadingPopup.Close();
                _loadingPopup = null;
                _isShowing = false;
            }
        }
    }
}