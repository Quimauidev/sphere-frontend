using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Graphics.ColorSpace;

namespace Sphere.Hubs
{
    public class PresenceService
    {
        private readonly string HubUrl = "https://sphere-iqm8.onrender.com";
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppNavigationService _anv;

        private HubConnection? _hubConnection;
        private Guid _currentUserId;

        public static Dictionary<Guid, bool> OnlineUsersCache { get; } = [];

        public event Action? Connected;

        public PresenceService(IServiceProvider serviceProvider, IAppNavigationService anv)
        {
            _serviceProvider = serviceProvider;
            _anv = anv;
        }

        public async Task StartAsync(Guid userId)
        {
            _currentUserId = userId;

            if (_hubConnection != null &&
                _hubConnection.State != HubConnectionState.Disconnected)
                return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{HubUrl}/presenceHub?userId={_currentUserId}")
                .WithAutomaticReconnect()
                .Build();

            RegisterHubEvents();

            await _hubConnection.StartAsync();

            Connected?.Invoke();
        }

        private void RegisterHubEvents()
        {
            if (_hubConnection == null) return;

            _hubConnection.On<List<Guid>>("OnlineUsers", userIds =>
            {
                foreach (var id in userIds)
                {
                    OnlineUsersCache[id] = true;

                    WeakReferenceMessenger.Default.Send(
                        new UserStatusChangedMessage(id, true));
                }

                WeakReferenceMessenger.Default.Send(
                    new AllOnlineUsersLoadedMessage());
            });

            _hubConnection.On<Guid, bool>("UserStatusChanged", (userId, isOnline) =>
            {
                OnlineUsersCache[userId] = isOnline;

                WeakReferenceMessenger.Default.Send(
                    new UserStatusChangedMessage(userId, isOnline));
            });

            _hubConnection.On<string>("ForceLogout", async message =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _anv.DisplayAlertAsync("Đăng nhập nơi khác", message);

                    await StopAsync();

                    PreferencesHelper.ClearAuthToken();
                    PreferencesHelper.ClearCurrentUser();

                    var login = _serviceProvider.GetRequiredService<LoginPage>();
                    _anv.SetRootPage(new NavigationPage(login));
                });
            });
        }

        public async Task StopAsync()
        {
            if (_hubConnection == null)
                return;

            if (_hubConnection.State != HubConnectionState.Disconnected)
                await _hubConnection.StopAsync();
        }
    }
}