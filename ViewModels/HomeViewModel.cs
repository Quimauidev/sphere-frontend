using AndroidX.Annotations;
using CommunityToolkit.Maui.ImageSources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Hubs;
using Sphere.Models;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.ViewModels.DiaryViewModels;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IDiaryService _diaryService;
        private readonly IFollowService _followService;
        private readonly IUserSessionService _userSessionService;
        private readonly IUserProfileService _userProfileService;
        private readonly IConversationService _conversationService;
        private readonly IShellNavigationService _nv;

        public ObservableCollection<DiaryFeedItemViewModel> FollowingPosts { get; } = [];
        public ObservableCollection<DiaryFeedItemViewModel> PopularPosts { get; } = [];
        public ObservableCollection<DiaryFeedItemViewModel> LatestPosts { get; } = [];

        [ObservableProperty] private UiViewState followingState;
        [ObservableProperty] private UiViewState popularState;
        [ObservableProperty] private UiViewState latestState;

        [ObservableProperty] private string? followingErrorMessage;
        [ObservableProperty] private string? popularErrorMessage;
        [ObservableProperty] private string? latestErrorMessage;

        [ObservableProperty] private bool isRefreshingFollowing;
        [ObservableProperty] private bool isRefreshingPopular;
        [ObservableProperty] private bool isRefreshingLatest;

        private const int PageSize = 20;

        // Following
        private int _followingPage = 1;

        private bool _followingLoading;
        private bool _followingHasNoMoreData;

        // Popular
        private int _popularPage = 1;

        private bool _popularLoading;
        private bool _popularHasNoMoreData;

        // Latest
        private int _latestPage = 1;

        private bool _latestLoading;
        private bool _latestHasNoMoreData;

        public HomeViewModel(IDiaryService diaryService, IFollowService followService, IUserSessionService userSessionService, IUserProfileService userProfileService, IConversationService conversationService, IShellNavigationService nv)
        {
            _diaryService = diaryService;
            _followService = followService;
            _userSessionService = userSessionService;
            _userProfileService = userProfileService;
            _conversationService = conversationService;
            _nv = nv;
            // Khởi động load
            _ = LoadFollowingAsync(forceReload: true);
            _ = LoadPopularAsync(forceReload: true);
            _ = LoadLatestAsync(forceReload: true);
            // 🔹 Khi có người online/offline realtime
            WeakReferenceMessenger.Default.Register<UserStatusChangedMessage>(this, (r, m) =>
            {
                void UpdateList(ObservableCollection<DiaryFeedItemViewModel> list)
                {
                    var user = list.FirstOrDefault(u => u.UserId == m.Value.UserId);
                    if (user != null)
                        user.IsOnline = m.Value.IsOnline;
                }

                UpdateList(FollowingPosts);
                UpdateList(PopularPosts);
                UpdateList(LatestPosts);
            });
            WeakReferenceMessenger.Default.Register<AllOnlineUsersLoadedMessage>(this, (r, m) =>
            {
                void RefreshList(ObservableCollection<DiaryFeedItemViewModel> list)
                {
                    foreach (var item in list)
                    {
                        if (PresenceService.OnlineUsersCache.TryGetValue(item.UserId, out bool isOnline))
                        {
                            item.IsOnline = isOnline;
                        }
                    }
                }

                RefreshList(FollowingPosts);
                RefreshList(PopularPosts);
                RefreshList(LatestPosts);
            });
            
        }

        [RelayCommand]
        public async Task LoadFirstFollowPage()
        {
            _followingPage = 1;
            FollowingPosts.Clear();
            await LoadFollowingAsync(forceReload: true);
        }

        [RelayCommand]
        public async Task LoadFirstPopularPage()
        {
            _popularPage = 1;
            PopularPosts.Clear();
            await LoadPopularAsync(forceReload: true);
        }

        [RelayCommand]
        public async Task LoadFirstLatestPage()
        {
            _latestPage = 1;
            LatestPosts.Clear();
            await LoadLatestAsync(forceReload: true);
        }

        // ----------------- FOLLOWING -----------------
        public async Task LoadFollowingAsync(bool forceReload = false)
        {
            if (_followingLoading || (_followingHasNoMoreData && !forceReload)) return;
            _followingLoading = true;
            IsRefreshingFollowing = forceReload;

            if (forceReload)
            {
                _followingPage = 1;
                _followingHasNoMoreData = false;
                FollowingPosts.Clear();
                FollowingState = UiViewState.Loading;
            }

            try
            {
                var response = await _diaryService.GetHomeDiariesAsync("follow", _followingPage, PageSize);

                if (response.IsSuccess)
                {
                    var currentUser = PreferencesHelper.LoadCurrentUser();
                    var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;

                    var items = response.Data?
                    .Select(d =>
                    {
                        var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv);
                        vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                         ? PresenceService.OnlineUsersCache[vm.UserId]
                         : d.UserDiaryDTO?.IsOnline ?? false;

                        return vm;
                    })
                    .ToList() ?? new List<DiaryFeedItemViewModel>();

                    if (forceReload) FollowingPosts.Clear();
                    foreach (var item in items) FollowingPosts.Add(item);
                    // nếu có data thì tăng page, còn không thì giữ nguyên page
                    if (items.Any())
                    {
                        _followingPage++;
                    }

                    _followingHasNoMoreData = items.Count < PageSize;

                    FollowingState = FollowingPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;
                    FollowingErrorMessage = FollowingPosts.Count == 0 ? response.Message : null;
                }
                else
                {
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    FollowingErrorMessage = msg;
                    FollowingState = response.Errors?.Any(e => e.Code is "NetworkError" or "Timeout" or "UnhandledException") == true
                        ? UiViewState.Offline
                        : UiViewState.Error;
                }
            }
            finally
            {
                _followingLoading = false;
                IsRefreshingFollowing = false;
            }
        }

        [RelayCommand]
        public Task LoadMoreFollowing() => LoadFollowingAsync();

        // ----------------- POPULAR -----------------
        public async Task LoadPopularAsync(bool forceReload = false)
        {
            if (_popularLoading || (_popularHasNoMoreData && !forceReload)) return;
            _popularLoading = true;
            IsRefreshingPopular = forceReload;

            if (forceReload)
            {
                _popularPage = 1;
                _popularHasNoMoreData = false;
                PopularPosts.Clear();
                PopularState = UiViewState.Loading;
            }

            try
            {
                var response = await _diaryService.GetHomeDiariesAsync("popular", _popularPage, PageSize);

                if (response.IsSuccess)
                {
                    var currentUser = PreferencesHelper.LoadCurrentUser();
                    var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;
                    var items = response.Data?
                    .Select(d =>
                    {
                        var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv);
                        vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                         ? PresenceService.OnlineUsersCache[vm.UserId]
                         : d.UserDiaryDTO?.IsOnline ?? false;

                        return vm;
                    })
                    .ToList() ?? new List<DiaryFeedItemViewModel>();

                    if (forceReload) PopularPosts.Clear();
                    foreach (var item in items) PopularPosts.Add(item);
                    // nếu có data thì tăng page, còn không thì giữ nguyên page
                    if (items.Any())
                    {
                        _popularPage++;
                    }

                    _popularHasNoMoreData = items.Count < PageSize;
                    

                    PopularState = PopularPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;
                    PopularErrorMessage = PopularPosts.Count == 0 ? response.Message : null;
                }
                else
                {
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    PopularErrorMessage = msg;
                    PopularState = response.Errors?.Any(e => e.Code is "NetworkError" or "Timeout" or "UnhandledException") == true
                        ? UiViewState.Offline
                        : UiViewState.Error;
                }
            }
            finally
            {
                _popularLoading = false;
                IsRefreshingPopular = false;
            }
        }

        [RelayCommand]
        public Task LoadMorePopular() => LoadPopularAsync();

        // ----------------- LATEST -----------------
        public async Task LoadLatestAsync(bool forceReload = false)
        {
            if (_latestLoading || (_latestHasNoMoreData && !forceReload)) return;
            _latestLoading = true;
            IsRefreshingLatest = forceReload;

            if (forceReload)
            {
                _latestPage = 1;
                _latestHasNoMoreData = false;
                LatestPosts.Clear();
                LatestState = UiViewState.Loading;
            }

            try
            {
                var response = await _diaryService.GetHomeDiariesAsync("latest", _latestPage, PageSize);

                if (response.IsSuccess)
                {
                    var currentUser = PreferencesHelper.LoadCurrentUser();
                    var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;
                    var items = response.Data?
                   .Select(d =>
                   {
                       var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv);
                       vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                         ? PresenceService.OnlineUsersCache[vm.UserId]
                         : d.UserDiaryDTO?.IsOnline ?? false;

                       return vm;
                   })
                   .ToList() ?? [];

                    if (forceReload) LatestPosts.Clear();
                    foreach (var item in items) LatestPosts.Add(item);
                    // nếu có data thì tăng page, còn không thì giữ nguyên page
                    if (items.Count != 0)
                    {
                        _latestPage++;
                    }

                    _latestHasNoMoreData = items.Count < PageSize;
                    

                    LatestState = LatestPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;
                    LatestErrorMessage = LatestPosts.Count == 0 ? response.Message : null;
                }
                else
                {
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    LatestErrorMessage = msg;
                    LatestState = response.Errors?.Any(e => e.Code is "NetworkError" or "Timeout" or "UnhandledException") == true
                        ? UiViewState.Offline
                        : UiViewState.Error;
                }
            }
            finally
            {
                _latestLoading = false;
                IsRefreshingLatest = false;
            }
        }

        [RelayCommand]
        public Task LoadMoreLatest() => LoadLatestAsync();
    }
}