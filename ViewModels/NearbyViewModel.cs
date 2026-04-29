using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ExCSS;
using IntelliJ.Lang.Annotations;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.DTOs;
using Sphere.Models;
using Sphere.Models.Params;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Icu.Util.LocaleData;

namespace Sphere.ViewModels
{
    public partial class NearbyViewModel : BaseViewModel
    {
        private readonly IUserSessionService _userSession;
        private readonly FilterService _filterService;
        // Biến kiểm soát task hiện tại để tránh chạy song song
        private readonly SemaphoreSlim _nearbyLock = new(1, 1);
        private readonly ApiResponseHelper _res;
        private readonly IFollowService _followService;
        private readonly INearbyService _nearbyService;
        private readonly IConversationService _conversationService;
        private readonly ILocationService _locationService;
        private readonly IPermissionService _permissionService;
        private CancellationTokenSource? _locationCts;
        private DateTime? _lastLoadTime;
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
        private bool _initialized;
        private int page = 1;
        private int pageSize = 20;
        private CancellationTokenSource? _nearbyCts;

        [ObservableProperty]
        private bool nearbyLoading;

        [ObservableProperty]
        private int distance;
        [ObservableProperty]
        private int minAge;
        [ObservableProperty]
        private int maxAge;
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isLocationEnabled;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private Gender? selectedGender;

        private bool _isEnablingLocation;
        private Location? _currentLocation;

        [ObservableProperty]
        private ObservableCollection<NearbyModel> nearby = [];
        public bool ShowLoading => IsLocationEnabled && IsLoading && !IsRefreshing;
        public bool ShowError => IsLocationEnabled && IsError;
        public bool ShowEmpty => IsLocationEnabled && IsEmpty;
        public bool ShowContent => IsLocationEnabled && IsSuccess;

        // Trạng thái đã lưu để biết đã tạo record vị trí trên server chưa, tránh trường hợp bật
        private static bool HasLocationRecord
        {
            get => PreferencesHelper.GetHasLocationRecord();
            set => PreferencesHelper.SetHasLocationRecord(value);
        }

        public NearbyViewModel(IUserSessionService userSessionService,IConversationService conversationService ,ApiResponseHelper res,INearbyService nearbyService, IFollowService followService, IPermissionService permissionService, IShellNavigationService nv, IAppNavigationService anv, ILocationService locationService, FilterService filterService)
        {
            _userSession = userSessionService;
            _conversationService = conversationService;
            _res = res;
            _nearbyService = nearbyService;
            _followService = followService;
            _permissionService = permissionService;
            _nv = nv;
            _anv = anv;
            _locationService = locationService;
            _filterService = filterService;
            // Load filter từ service
            SelectedGender = _filterService.SelectedGender;
            MinAge = _filterService.MinAge;
            MaxAge = _filterService.MaxAge;
            Distance = _filterService.Distance;
            // 🔹 Đọc trạng thái đã lưu
            IsLocationEnabled = PreferencesHelper.GetLocationEnabled();
            WeakReferenceMessenger.Default.Register<FollowChangedMessage>(this, (r, m) =>
            {
                var item = Nearby.FirstOrDefault(x => x.UserId == m.Value);
                item?.IsFollowing = true;
            });
        }   
        
        [ObservableProperty]
        public partial UserWithUserProfileModel? CurrentUser { get; set; }
        // Phương thức khởi tạo để gọi sau khi tạo instance
        public async Task InitAsync()
        {
            if (_initialized)
                return;

            _initialized = true;
            if (IsLocationEnabled)
            {
                UiState = UiViewState.Loading;
                _locationCts = new CancellationTokenSource();
                await EnableLocationAsync(_locationCts.Token);
            }
        }

        // Khi IsLocationEnabled thay đổi, lưu trạng thái và thực hiện enable/disable location
        partial void OnIsLocationEnabledChanged(bool value)
        {
            PreferencesHelper.SetLocationEnabled(value);
            try
            {
                _locationCts?.Cancel();
                _locationCts?.Dispose();
            }
            catch { }
            if (value)
            {
                // Khi bật lại vị trí, reset state và bắt đầu loading
                UiState = UiViewState.Loading;
                _locationCts = new CancellationTokenSource();
                _ = EnableLocationAsync(_locationCts.Token);
            }
            else
            {
                // Khi tắt vị trí, reset state và bắt đầu loading
                UiState = UiViewState.Idle;
                _ = DisableLocationAsync();
            }

            OnPropertyChanged(nameof(ShowLoading));
            OnPropertyChanged(nameof(ShowError));
            OnPropertyChanged(nameof(ShowEmpty));
            OnPropertyChanged(nameof(ShowContent));
        }
        
        // Phương thức enable location với kiểm soát để tránh chạy song song
        private async Task EnableLocationAsync(CancellationToken token)
        {
            if (_isEnablingLocation) return;

            _isEnablingLocation = true;

            try
            {
                UiState = UiViewState.Loading;

                if (!await CheckPermissionAndGps())
                {
                    IsLocationEnabled = false;
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không thể truy cập GPS.";
                    return;
                }
                _currentLocation = await GetAccurateLocationAsync(token);
                if (_currentLocation == null)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không lấy được vị trí.";
                    return;
                }
                await EnsureLocationRecord();

                await ReloadNearby();
            }
            finally
            {
                _isEnablingLocation = false;
            }
        }

        public async Task<bool> CheckPermissionAndGps()
        {
            var permission = await _permissionService.RequestPermissionAsync(AppPermission.Location);

            if (permission != PermissionResult.Granted)
            {
                IsLocationEnabled = false;
                return false;
            }

            var gpsOn = _permissionService.IsGpsEnabled();
            if (!gpsOn)
            {
                IsLocationEnabled = false;
                await _permissionService.ShowGpsDialogAsync();
                return false;
            }
            IsLocationEnabled = true; // 🔥 DÒNG QUAN TRỌNG
            return true;
        }

        // Kiểm tra nếu đã có record vị trí, nếu có thì update, nếu chưa thì tạo mới
        private async Task EnsureLocationRecord()
        {
            if (!HasLocationRecord)
            {
                await CreateLocationAsync();
            }
            else
            {
                await _locationService.SetLocationVisibilityAsync(true);
            }
        }

        // phương thức update vị chung
        private async Task CreateLocationAsync()
        {
            var resp = await _locationService.CreateLocationAsync(new CreateLocationRequest
            {
                Latitude = _currentLocation!.Latitude,
                Longitude = _currentLocation!.Longitude
            });

            if (resp.IsSuccess)
                HasLocationRecord = true;
        }

        // Phương thức lấy vị trí chính xác, ưu tiên last known nếu đủ tốt, nếu không thì lấy mới với timeout

        private async Task<Location?> GetAccurateLocationAsync(CancellationToken token, int samples = 5, int maxAccuracyMeters = 80)
        {
            try
            {
                var last = await Geolocation.Default.GetLastKnownLocationAsync();
                if (last != null && last.Accuracy <= maxAccuracyMeters && DateTimeOffset.UtcNow - last.Timestamp < TimeSpan.FromSeconds(15))
                    return last;

                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

                var locations = new List<Location>();

                for (int i = 0; i < samples; i++)
                {
                    token.ThrowIfCancellationRequested();

                    if (!_permissionService.IsGpsEnabled())
                        return null;

                    var loc = await Geolocation.Default.GetLocationAsync(request, token);

                    if (loc?.Accuracy > 0)
                        locations.Add(loc);

                    await Task.Delay(800, token);
                }

                if (locations.Count == 0)
                    return null;

                var filtered = locations
                    .Where(l => l.Accuracy <= maxAccuracyMeters)
                    .ToList();

                if (filtered.Count == 0)
                    filtered = locations;

                var best = filtered.OrderBy(l => l.Accuracy).First();
                return new Location
                {
                    Latitude = Math.Round(best.Latitude, 6),
                    Longitude = Math.Round(best.Longitude, 6),
                    Accuracy = best.Accuracy,
                    Timestamp = best.Timestamp
                };
            }
            catch
            {
                return null;
            }
        }

        [RelayCommand]
        // Phương thức reload dữ liệu nearby, reset paging và trạng thái
        private async Task ReloadNearby()
        {
            page = 1;
            Nearby.Clear();
            await LoadNearby(forceReload:true);
        }

        // Phương thức load dữ liệu nearby, kiểm soát để tránh chạy song song, và xử lý paging
        private async Task LoadNearby(bool forceReload = false)
        {
            if (!IsLocationEnabled)
            {
                _nearbyCts?.Cancel();
                return;
            }
            if (NearbyLoading)
                return;
            if (HasNoMoreData && !forceReload)
                return;
            if (!await _nearbyLock.WaitAsync(0))
                return;
            
            try
            {
                NearbyLoading = true;
                if (forceReload)
                {
                    page = 1;
                    HasNoMoreData = false;
                    Nearby.Clear();
                    if (!IsRefreshing)
                        UiState = UiViewState.Loading;
                }
                _nearbyCts?.Cancel();
                _nearbyCts?.Dispose();
                _nearbyCts = new CancellationTokenSource();
                var token = _nearbyCts.Token;
                var req = new NearbyRequest
                {
                    Latitude = _currentLocation!.Latitude,
                    Longitude = _currentLocation.Longitude,
                    DistanceKm = Distance,
                    Page = page,
                    PageSize = pageSize,
                    Gender = SelectedGender,
                    MinAge = MinAge,
                    MaxAge = MaxAge
                };
                var resp = await _nearbyService.GetNearbyUsersAsync(req, token);
                if (token.IsCancellationRequested)
                    return; // 🔥 bỏ kết quả request cũ

                if (!resp.IsSuccess)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = resp.Errors?.FirstOrDefault()?.Description ?? resp.Message ?? "Có lỗi xảy ra";
                    return;
                }

                var data = resp.Data ?? [];
              

                foreach (var item in data)
                    Nearby.Add(item);
                
                if (data.Any())
                {
                    page++;
                }
                HasNoMoreData = data.Count() < pageSize;
                _lastLoadTime = DateTime.UtcNow;

                UiState = nearby.Count == 0 ? UiViewState.Empty : UiViewState.Success;
            }
            finally
            {
                NearbyLoading = false;
                IsRefreshing = false;
                _nearbyLock.Release();
            }
        }

        // Phương thức disable location, ẩn vị trí trên server và xóa danh sách nearby
        private async Task DisableLocationAsync()
        {
            try
            {
                await _locationService.SetLocationVisibilityAsync(false);
                Nearby.Clear();
            }
            catch (Exception ex)
            {
                await _anv.DisplayAlertAsync("Lỗi", ex.Message);
            }
        }

        // Command để load thêm dữ liệu nearby khi cuộn đến cuối danh sách
        [ObservableProperty]
        private bool isLoadingMore;

        [RelayCommand]
        public async Task LoadMoreNearby()
        {
            if (HasNoMoreData || IsLoadingMore) return;

            IsLoadingMore = true;

            try
            {
                await LoadNearby();
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

       

        // Command để refresh lại danh sách nearby, reset paging và trạng thái
        [RelayCommand]
        public async Task RefreshNearby(CancellationToken token)
        {
            IsRefreshing = true;

            try
            {
                _currentLocation = await GetAccurateLocationAsync(token);
                if (_currentLocation == null)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không lấy được vị trí.";
                    return;
                }

                if (!HasLocationRecord)
                {
                    await CreateLocationAsync();
                }
                else
                {
                    await _locationService.CreateLocationAsync(new CreateLocationRequest
                    {
                        Latitude = _currentLocation.Latitude,
                        Longitude = _currentLocation.Longitude
                    });
                }
                await ReloadNearby();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        // Command để mở trang filter, và nhận kết quả filter để cập nhật lại danh sách nearby
        [RelayCommand]
        public async Task Filter()
        {
            if (!IsLocationEnabled)
            {
                await _anv.DisplayAlertAsync("Thông báo", "Vui lòng bật vị trí để lọc lân cận");
                return;
            }
            var param = new FilterParam
            {
                SelectedGender = SelectedGender,
                MinAge = MinAge,
                MaxAge = MaxAge,
                Distance = Distance,
                OnApply = (gender, minAge, maxAge, dist) =>
                {
                    SelectedGender = gender;
                    MinAge = minAge;
                    MaxAge = maxAge;
                    Distance = dist;
                    _filterService.SelectedGender = gender;
                    _filterService.MinAge = minAge;
                    _filterService.MaxAge = maxAge;
                    _filterService.Distance = dist;

                    UiState = UiViewState.Loading;
                    // reload dữ liệu với filter mới
                    _ = ReloadNearby();
                }
            };

            await _nv.PushModalAsync<FilterPage, FilterParam>(param);
        }

        [RelayCommand]
        public async Task OpenProfile(NearbyModel user)
        {
            if (user == null) return;
            await PopupHelper.ShowLoadingAsync();
            await _nv.PushModalAsync<ProfilePage, Guid?>(user.UserId);
        }

        [RelayCommand]
        public async Task Follow(NearbyModel user)
        {
            if (user == null || user.IsFollowing) return;

            user.IsBusy = true;
            await PopupHelper.ShowLoadingAsync("Đang theo dõi...");
            try
            {
                var res = await _followService.FollowUserAsync(user.UserId);

                if (res.IsSuccess)
                {
                    user.IsFollowing = true; // 🔥 quan trọng nhất
                }
                else
                {
                    await _res.ShowApiErrorsAsync(res, "Theo dõi thất bại");
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                user.IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task Chat(NearbyModel user)
        {
            if (user == null || user.UserId == Guid.Empty)
                return;

            if (IsBusy)
                return;
            var targetUserId = user.UserId;
            var myId = _userSession.CurrentUser?.UserDTO?.Id;
            if (myId == null || myId.Value == targetUserId)
                return;
            IsBusy = true;

            try
            {
                bool alreadyUnlocked = PreferencesHelper.IsChatUnlocked(myId.Value, targetUserId);

                if (alreadyUnlocked)
                {
                    await OpenChatAsync(targetUserId, user);
                    return;
                }

                var check = await _conversationService.CheckConversationAsync(targetUserId);
                if (!check.IsSuccess)
                {
                    await _res.ShowApiErrorsAsync(check, "Không thể kiểm tra");
                    return;
                }

                var data = check.Data;
                if (data!.IsUnlocked)
                {
                    await OpenChatAsync(data.ConversationId, user);
                    return;
                }

                bool confirm = await ApiResponseHelper.ShowShellConfirmAsync(
                    "Xác nhận mở khóa",
                    "Cần tiêu 130 kim cương 💎 để mở khóa cuộc trò chuyện này. Bạn có muốn tiếp tục không?",
                    "Đồng ý",
                    "Hủy");

                if (!confirm)
                    return;

                await PopupHelper.ShowLoadingAsync("Đang mở khóa...");

                var response = await _conversationService.StartConversationAsync(targetUserId);

                await PopupHelper.HideLoadingAsync();

                if (response.Errors?.Any(e => e.Code == "NotEnoughDiamonds") == true)
                {
                    bool goTopUp = await ApiResponseHelper.ShowShellConfirmAsync(
                        "Không đủ kim cương 💎",
                        "Bạn không đủ kim cương. Nạp ngay?",
                        "Nạp ngay",
                        "Đóng");

                    if (goTopUp)
                        await _nv.PushModalAsync<DiamondPage>();

                    return;
                }

                if (!response.IsSuccess)
                {
                    await _res.ShowApiErrorsAsync(response, "Không thể mở chat");
                    return;
                }

                PreferencesHelper.SetChatUnlocked(myId.Value, targetUserId, true);

                if (response.Data!.IsFirstUnlock)
                {
                    UpdateCoins(response.Data.NewBalance);

                    await _anv.DisplayAlertAsync(
                        "Mở khóa thành công",
                        $"Còn lại: {response.Data.NewBalance} 💎");
                }

                await OpenChatAsync(response.Data.ConversationId, user);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async Task OpenChatAsync(Guid? conId, NearbyModel user)
        {
            await _nv.PushModalAsync<MessagePage, MessageNavigationParam>(
                new MessageNavigationParam
                {
                    ConversationId = conId ?? Guid.Empty,
                    Partner = new UserDiaryModel
                    {
                        Id = user.UserId,
                        FullName = user.FullName,
                        AvatarUrl = user.AvatarUrl,
                        Gender = user.Gender,
                        IsOnline = false,
                        IsFollow = user.IsFollowing
                    }
                });
        }

        private void UpdateCoins(long newBalance)
        {
            var myUser = _userSession.CurrentUser;

            if (myUser?.UserProfileDTO == null) return;

            // update user thật
            myUser.UserProfileDTO.Coins = newBalance;
            CurrentUser = myUser; // trigger UI luôn
            _userSession.CurrentUser = myUser;
            PreferencesHelper.SaveCurrentUser(myUser);
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(UiState))
            {
                OnPropertyChanged(nameof(ShowLoading));
                OnPropertyChanged(nameof(ShowError));
                OnPropertyChanged(nameof(ShowEmpty));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }
}