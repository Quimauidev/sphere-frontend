using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Services.IService
{
    public interface IAppNavigationService
    {
        void SetRootPage(Page page);
        Task DisplayAlertAsync(string title, string message, string cancel = "OK");
        Task<string> ShowActionSheetAsync( string title, string cancel, string? destruction, params string[] buttons);
        Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel);
    }
}
