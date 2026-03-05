using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Services.Service
{
    internal class AppNavigationService : IAppNavigationService
    {
        public void SetRootPage(Page page)
        {
            Application.Current!.Windows[0].Page = page;
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            var app = Application.Current ?? throw new InvalidOperationException("Application.Current is null");
            var window = app.Windows.Count > 0
                ? app.Windows[0]
                : throw new InvalidOperationException("No active window");

            return await window.Page!.DisplayActionSheetAsync( title, cancel, destruction, buttons);
        }
    }
}
