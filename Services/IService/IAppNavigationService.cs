using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Services.IService
{
    public interface IAppNavigationService
    {
        void SetRootPage(Page page);
        Task<string> ShowActionSheetAsync( string title, string cancel, string? destruction, params string[] buttons);
    }
}
