using Sphere.Interfaces;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class ShellNavigationService(IServiceProvider serviceProvider) : IShellNavigationService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public INavigation Navigation => Application.Current!.MainPage!.Navigation;

        // =====================
        // Push / Pop
        // =====================
        public async Task PushAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PushAsync(page));
        }

        public async Task PushAsync(Page page)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PushAsync(page));
        }

        public async Task PopAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PopAsync());
        }

        // =====================
        // Modal
        // =====================
        public async Task PushModalAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PushModalAsync(page));
        }

        public async Task PushModalAsync(Page page)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PushModalAsync(page));
        }

        public async Task PopModalAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PopModalAsync());
        }

        // =====================
        // Root
        // =====================
        public async Task ReplaceRootAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage = page);
        }

        public async Task ReplaceRootAsync(Page page)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage = page);
        }

        public async Task ClearAndPushAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage = new NavigationPage(page));
        }

        // =====================
        // Shell
        // =====================
        public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (Shell.Current == null)
                throw new InvalidOperationException("Shell is not initialized");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (parameters == null)
                    await Shell.Current.GoToAsync(route);
                else
                    await Shell.Current.GoToAsync(route, parameters);
            });
        }

        public async Task PushModalAsync<TPage, TParam>(TParam param) where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();

            // 👉 Gán param vào ViewModel
            if (page.BindingContext is IModalParameterReceiver<TParam> receiver)
            {
                receiver.Receive(param);
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
                Navigation.PushModalAsync(page));
        }
    }
}
