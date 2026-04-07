using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExCSS;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.DTOs;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Icu.Util.LocaleData;

namespace Sphere.ViewModels
{
    public partial class NearbyViewModel : BaseViewModel
    {
        private readonly FilterService _filterService;
        // Biến kiểm soát task hiện tại để tránh chạy song song
        private readonly SemaphoreSlim _nearbyLock = new(1, 1);

        private readonly INearbyService _nearbyService;
        private readonly ILocationService _locationService;
        private readonly IPermissionService _permissionService;
        private CancellationTokenSource? _locationCts;
        private DateTime? _lastLoadTime;
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
        private bool _initialized;
        private int _page = 1;
        private const int _pageSize = 20;
        private CancellationTokenSource? _nearbyCts;

        private bool _noMoreData;

        [ObservableProperty]
        private bool nearbyLoading;

        [ObservableProperty]
        private int distance;
        [ObservableProperty]
        private int minAge;
        [ObservableProperty]
        private int maxAge;


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

        // Trạng thái đã lưu để biết đã tạo record vị trí trên server chưa, tránh trường hợp bật
        private static bool HasLocationRecord
        {
            get => PreferencesHelper.GetHasLocationRecord();
            set => PreferencesHelper.SetHasLocationRecord(value);
        }

        public NearbyViewModel(INearbyService nearbyService, IPermissionService permissionService, IShellNavigationService nv, IAppNavigationService anv, ILocationService locationService, FilterService filterService)
        {
            _nearbyService = nearbyService;
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
        }

        // Phương thức khởi tạo để gọi sau khi tạo instance
        public async Task InitAsync()
        {
            if (_initialized)
                return;

            _initialized = true;
            if (IsLocationEnabled)
            {
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
                _locationCts = new CancellationTokenSource();
                _ = EnableLocationAsync(_locationCts.Token);
            }
            else
                _ = DisableLocationAsync();
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
                return false;

            if (!_permissionService.IsGpsEnabled())
            {
                await _permissionService.ShowGpsDialogAsync();
                return false;
            }

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

        // Phương thức reload dữ liệu nearby, reset paging và trạng thái
        private async Task ReloadNearby()
        {
            _page = 1;
            _noMoreData = false;
            Nearby.Clear();

            await LoadNearby();
        }

        // Phương thức load dữ liệu nearby, kiểm soát để tránh chạy song song, và xử lý paging
        private async Task LoadNearby()
        {
            if (NearbyLoading || _noMoreData || !IsLocationEnabled)
                return;

            if (!await _nearbyLock.WaitAsync(0))
                return;

            try
            {
                NearbyLoading = true;
                _nearbyCts?.Cancel();
                _nearbyCts?.Dispose();
                _nearbyCts = new CancellationTokenSource();
                var token = _nearbyCts.Token;
                var req = new NearbyRequest
                {
                    Latitude = _currentLocation!.Latitude,
                    Longitude = _currentLocation.Longitude,
                    DistanceKm = Distance,
                    Page = _page,
                    PageSize = _pageSize,
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
                    ErrorMessage = resp.Message;
                    return;
                }

                var data = resp.Data ?? [];
                if (!data.Any())
                {
                    if (_page == 1)
                        UiState = UiViewState.Empty;

                    _noMoreData = true; // đánh dấu không còn dữ liệu để load nữa
                    return;
                }

                foreach (var item in data)
                    Nearby.Add(item);
                if (data.Count() < _pageSize)
                {
                    _noMoreData = true;
                }
                _page++;

                _lastLoadTime = DateTime.UtcNow;

                UiState = UiViewState.Success;
            }
            catch (TaskCanceledException)
            {
                // 🔥 ignore (bị cancel là bình thường)
            }
            finally
            {
                NearbyLoading = false;
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
            if (_noMoreData || NearbyLoading || IsLoadingMore)
                return;

            try
            {
                IsLoadingMore = true;
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
                await _anv.DisplayAlertAsync("Thông báo", "Vui lòng bật định vị để lọc lân cận");
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

    }
}