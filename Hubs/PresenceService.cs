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
        private readonly string _hubUrl;
        private readonly Guid _currentUserId;
        private readonly IServiceProvider _serviceProvider;
        private readonly HubConnection _hubConnection;
        private readonly IAppNavigationService _anv;

        public static Dictionary<Guid, bool> OnlineUsersCache { get; } = [];

        public event Action? Connected;

        public PresenceService(string hubUrl, Guid currentUserId, IServiceProvider serviceProvider, IAppNavigationService anv)
        {
            _hubUrl = hubUrl;
            _currentUserId = currentUserId;
            _serviceProvider = serviceProvider;
            _anv = anv;
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_hubUrl}/presenceHub?userId={_currentUserId}")
                .WithAutomaticReconnect()
                .Build();

            // 🔹 Danh sách người online hiện tại
            _hubConnection.On<List<Guid>>("OnlineUsers", userIds =>
            {
                foreach (var id in userIds)
                {
                    OnlineUsersCache[id] = true;
                    WeakReferenceMessenger.Default.Send(new UserStatusChangedMessage(id, true));
                }
                // 🔔 Báo cho ViewModel biết đã load xong toàn bộ
                WeakReferenceMessenger.Default.Send(new AllOnlineUsersLoadedMessage());
            });

            // 🔹 Khi có ai online/offline
            _hubConnection.On<Guid, bool>("UserStatusChanged", (userId, isOnline) =>
            {
                OnlineUsersCache[userId] = isOnline;
                WeakReferenceMessenger.Default.Send(new UserStatusChangedMessage(userId, isOnline));
            });

            // 🔹 Khi bị đăng xuất do login ở nơi khác
            _hubConnection.On<string>("ForceLogout", async (message) =>
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

        public async Task StartAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                Connected?.Invoke();
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
            }
        }
    }
}