using CommunityToolkit.Maui.Views;
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

        private static Window Window => Application.Current?.Windows[0] ?? throw new InvalidOperationException("No active window");
        private static INavigation Navigation => Window.Page?.Navigation ?? throw new InvalidOperationException("Navigation not available");

        // =====================
        // Push / Pop
        // =====================
        public async Task PushAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await Navigation.PushAsync(page);
        }

        public async Task PopAsync()
        {
            await Navigation.PopAsync();
        }

        // =====================
        // Modal
        // =====================
        public async Task PushModalAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            await Navigation.PushModalAsync(page);
        }
        public async Task PushModalAsync<TPage, TParam>(TParam param) where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();    

            // 👉 Gán param vào ViewModel
            if (page.BindingContext is IModalParameterReceiver<TParam> receiver)
            {
               await receiver.Receive(param);
            }

            await Navigation.PushModalAsync(page);
        }

        public async Task PopModalAsync()
        {
            await Navigation.PopModalAsync();
        }

        // =====================
        // Root
        // =====================
        public Task ReplaceRootAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            Window.Page = page;
            return Task.CompletedTask;
        }

        public Task ClearAndPushAsync<TPage>() where TPage : Page
        {
            var page = _serviceProvider.GetRequiredService<TPage>();
            Window.Page = new NavigationPage(page);
            return Task.CompletedTask;
        }

        // =====================
        // Shell
        // =====================

        

        public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (Shell.Current == null)
                throw new InvalidOperationException("Shell is not initialized");

            if (parameters == null)
                await Shell.Current.GoToAsync(route);
            else
                await Shell.Current.GoToAsync(route, parameters);
        }

        public async Task<object?> ShowPopupAsync(Popup popup)
        {
            return await Window.Page!.ShowPopupAsync(popup);
        }
    }
}
