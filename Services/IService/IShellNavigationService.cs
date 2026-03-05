using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.IService
{
    public interface IShellNavigationService
    {
        // ===== Navigation stack =====
        Task PushAsync<TPage>() where TPage : Page;
        Task PopAsync();

        // ===== Modal =====
        Task PushModalAsync<TPage, TParam>(TParam param) where TPage : Page;
        Task PushModalAsync<TPage>() where TPage : Page;
        Task PopModalAsync();

        // ===== Root / Reset =====
        Task ReplaceRootAsync<TPage>() where TPage : Page;
        Task ClearAndPushAsync<TPage>() where TPage : Page;

        // ===== Shell specific =====
        Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
        Task<object?> ShowPopupAsync(Popup popup);
    }
}
