using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Services.Service
{
    internal class AppNavigationService : IAppNavigationService
    {
        private Page GetCurrentPage()
        {
            var app = Application.Current
                ?? throw new InvalidOperationException("Application.Current is null");

            if (app.Windows.Count == 0)
                throw new InvalidOperationException("No active window");

            return app.Windows[0].Page
                ?? throw new InvalidOperationException("Page is null");
        }

        public void SetRootPage(Page page)
        {
            var app = Application.Current
            ?? throw new InvalidOperationException("Application.Current is null");

            if (app.Windows.Count == 0)
                throw new InvalidOperationException("No active window");

            app.Windows[0].Page = page;
        }

        // cách chuẩn để hiển thị alert từ bất kỳ đâu trong app mà không cần lo về context
        public async Task DisplayAlertAsync(string title, string message, string cancel = "OK")
        {
            var page = GetCurrentPage();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await page.DisplayAlertAsync(title, message, cancel);
            });
        }

        public async Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            var page = GetCurrentPage();

            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                return await page.DisplayActionSheetAsync(title, cancel, destruction, buttons);
            });
        }

        public async Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
        {
            var page = GetCurrentPage();

            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                return await page.DisplayAlertAsync(title, message, accept, cancel);
            });
        }
    }
}
