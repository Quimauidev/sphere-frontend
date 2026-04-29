using AndroidX.Annotations;
using CommunityToolkit.Maui.ImageSources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
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
    public partial class HomeViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;
        private readonly IFollowService _followService;
        private readonly IUserSessionService _userSessionService;
        private readonly IUserProfileService _userProfileService;
        private readonly IConversationService _conversationService;
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
        private readonly ApiResponseHelper _res;

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
        private CancellationTokenSource? _latestCts;
        private CancellationTokenSource? _popularCts;
        private CancellationTokenSource? _followingCts;

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

        public HomeViewModel(IDiaryService diaryService, IFollowService followService, IUserSessionService userSessionService, IUserProfileService userProfileService, IConversationService conversationService, IShellNavigationService nv, IAppNavigationService anv, ApiResponseHelper res)
        {
            _diaryService = diaryService;
            _followService = followService;
            _userSessionService = userSessionService;
            _userProfileService = userProfileService;
            _conversationService = conversationService;
            _nv = nv;
            _anv = anv;
            _res = res;
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
                    user?.IsOnline = m.Value.IsOnline;
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
            if (forceReload)
            {
                _followingPage = 1;
                _followingHasNoMoreData = false;
                FollowingPosts.Clear();
                if (!IsRefreshingFollowing)
                    FollowingState = UiViewState.Loading;   
            }

            try
            {
                _followingCts?.Cancel();
                _followingCts?.Dispose();
                _followingCts = new CancellationTokenSource();
                var token = _followingCts.Token;
                var response = await _diaryService.GetHomeDiariesAsync("follow", _followingPage, PageSize, token);
                if(!response.IsSuccess)
                {
                    FollowingState = UiViewState.Error;
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    FollowingErrorMessage = msg;
                    return;
                }
                var currentUser = PreferencesHelper.LoadCurrentUser();
                var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;

                var data = response.Data?
                .Select(d =>
                {
                    var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv, _anv, _res, _userSessionService);
                    vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                     ? PresenceService.OnlineUsersCache[vm.UserId]
                     : d.UserDiaryDTO?.IsOnline ?? false;

                    return vm;
                })
                .ToList() ?? [];
               
                foreach (var item in data)
                    FollowingPosts.Add(item);
                // nếu có data thì tăng page, còn không thì giữ nguyên page
                
                if (data.Count != 0)
                {
                    _followingPage++;
                }
                _followingHasNoMoreData = data.Count < PageSize;
                FollowingState = FollowingPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;
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
            if (forceReload)
            {
                _popularPage = 1;
                _popularHasNoMoreData = false;
                PopularPosts.Clear();
                if(!IsRefreshingPopular)
                    PopularState = UiViewState.Loading;
            }
           
            try
            {
                _popularCts?.Cancel();
                _popularCts?.Dispose();
                _popularCts = new CancellationTokenSource();
                var token = _popularCts.Token;
                var response = await _diaryService.GetHomeDiariesAsync("popular", _popularPage, PageSize, token);
                if(!response.IsSuccess)
                {
                    PopularState = UiViewState.Error;
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    PopularErrorMessage = msg;
                }
                var currentUser = PreferencesHelper.LoadCurrentUser();
                var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;
                var data = response.Data?
                .Select(d =>
                {
                    var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv, _anv, _res, _userSessionService);
                    vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                     ? PresenceService.OnlineUsersCache[vm.UserId]
                     : d.UserDiaryDTO?.IsOnline ?? false;

                    return vm;
                })
                .ToList() ?? new List<DiaryFeedItemViewModel>();
               
                foreach (var item in data)
                    PopularPosts.Add(item);
                // nếu có data thì tăng page, còn không thì giữ nguyên page

                if (data.Count != 0)
                {
                    _popularPage++;
                }

                _popularHasNoMoreData = data.Count < PageSize;

                PopularState = PopularPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;

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
            if (forceReload)
            {
                _latestPage = 1;
                _latestHasNoMoreData = false;
                LatestPosts.Clear();
                if(!IsRefreshingLatest)
                    LatestState = UiViewState.Loading;
            }
            
            try
            {
                _latestCts?.Cancel();
                _latestCts?.Dispose();
                _latestCts = new CancellationTokenSource();
                var token = _latestCts.Token;
                var response = await _diaryService.GetHomeDiariesAsync("latest", _latestPage, PageSize, token);
                if(!response.IsSuccess)
                {
                    LatestState = UiViewState.Error;
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    LatestErrorMessage = msg;
                    return;
                }
                var currentUser = PreferencesHelper.LoadCurrentUser();
                var currentUserId = currentUser?.UserDTO?.Id ?? Guid.Empty;
                var data = response.Data?
               .Select(d =>
               {
                   var vm = new DiaryFeedItemViewModel(d, currentUserId, _followService, _conversationService, _nv, _anv, _res, _userSessionService);
                   vm.IsOnline = PresenceService.OnlineUsersCache.ContainsKey(vm.UserId)
                     ? PresenceService.OnlineUsersCache[vm.UserId]
                     : d.UserDiaryDTO?.IsOnline ?? false;

                   return vm;
               })
               .ToList() ?? [];

                foreach (var item in data)
                    LatestPosts.Add(item);
                // nếu có data thì tăng page, còn không thì giữ nguyên page
                if (data.Count != 0)
                {
                    _latestPage++;
                }

                _latestHasNoMoreData = data.Count < PageSize;


                LatestState = LatestPosts.Count == 0 ? UiViewState.Empty : UiViewState.Success;
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