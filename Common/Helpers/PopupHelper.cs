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
        private static bool _isShowing;

        private static Page? GetCurrentPage()
        {
            return Shell.Current ?? Application.Current?.MainPage;
        }

        public static Task ShowLoadingAsync()
        {
            if (_isShowing)
                return Task.CompletedTask;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var page = GetCurrentPage();
                if (page == null)
                    return;

                _isShowing = true;
                _loadingPopup = new LoadingPopup();

                page.ShowPopup(_loadingPopup); // ✅ KHÔNG await
            });

            return Task.CompletedTask;
        }


        public static async Task HideLoadingAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_loadingPopup != null)
                {
                    _loadingPopup.Close();
                    _loadingPopup = null;
                }

                _isShowing = false;
            });
        }
    }
}