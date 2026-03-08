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
        // Biến kiểm soát task hiện tại để tránh chạy song song
        private readonly SemaphoreSlim _nearbyLock = new(1, 1);
        private readonly INearbyService _nearbyService;
        private readonly IPermissionService _permissionService;
        private CancellationTokenSource? _locationCts;
        private DateTime? _lastLoadTime;
        private readonly TimeSpan _minLoadInterval = TimeSpan.FromSeconds(30);
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
       
        private const int _pageSize = 20;
        private int _page = 1;

        private bool _noMoreData;
        [ObservableProperty]
        private bool nearbyLoading;

        [ObservableProperty]
        private int distance = 60; // khoảng cách mặc định 60 km

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
        private bool HasLocationRecord
        {
            get => PreferencesHelper.GetHasLocationRecord();
            set => PreferencesHelper.SetHasLocationRecord(value);
        }

        public NearbyViewModel( INearbyService nearbyService, IPermissionService permissionService, IShellNavigationService nv, IAppNavigationService anv)
        {
            _nearbyService = nearbyService;
            _permissionService = permissionService;
            _nv = nv;
            _anv = anv;
            // 🔹 Đọc trạng thái đã lưu
            IsLocationEnabled = PreferencesHelper.GetLocationEnabled();
            
        }
        //private async Task<bool> RequestLocationPermissionAndGpsAsync()
        //{
        //    var permissionResult = await _permissionService.RequestPermissionAsync(AppPermission.Location);
        //    if (permissionResult != PermissionResult.Granted)
        //        return false;
        //    if (!_permissionService.IsGpsEnabled())
        //    {
        //        await _permissionService.ShowGpsDialogAsync();
        //        return false;
        //    }
        //    return true;
        //}

        //private async Task<Location?> GetAccurateLocationAsync(int samples = 3, int maxAccuracyMeters = 80)
        //{
        //    var last = await Geolocation.Default.GetLastKnownLocationAsync();
        //    if (last != null && last.Accuracy <= maxAccuracyMeters)
        //        return last;
        //    var locs = new List<Location>();
        //    var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(8));
        //    for (int i = 0; i < samples; i++)
        //    {
        //        if (!_permissionService.IsGpsEnabled())
        //            return null;
        //        try
        //        {
        //            var loc = await Geolocation.Default.GetLocationAsync(request);
        //            if (loc != null && loc.Accuracy <= maxAccuracyMeters)
        //                locs.Add(loc);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Lỗi lấy GPS: {ex.Message}");
        //        }
        //        await Task.Delay(700);
        //    }
        //    if (locs.Count == 0)
        //        return null;
        //    return locs.OrderBy(l => l.Accuracy).First();
        //}
        // Phương thức khởi tạo để gọi sau khi tạo instance
        public async Task InitAsync()
        {
            _currentLocation = null;
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

            _locationCts?.Cancel();
            _locationCts?.Dispose();
            _locationCts = new CancellationTokenSource();

            if (value)
                _ = EnableLocationAsync(_locationCts.Token);
            else
                _ = DisableLocationAsync();
        }
        // Phương thức enable location với kiểm soát để tránh chạy song song
        private async Task EnableLocationAsync(CancellationToken token)
        {
            Console.WriteLine("EnsureLocationRecord called");
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
                _currentLocation = await GetAccurateLocationAsync();
                Console.WriteLine($"[GPS] EnableLocationAsync: {_currentLocation?.Latitude}, {_currentLocation?.Longitude}");
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
            Console.WriteLine($"HasLocationRecord: {HasLocationRecord}");
            if (!HasLocationRecord)
            {
                await CreateLocationAsync();
            }
            else
            {
                Console.WriteLine("CALL SetLocationVisibilityAsync(true)");
                await _nearbyService.SetLocationVisibilityAsync(true);
            }
            
        }
        // phương thức update vị chung
        private async Task CreateLocationAsync()
        {
            
            var resp = await _nearbyService.CreateLocationAsync(new CreateLocationRequest
            {
                Latitude = _currentLocation!.Latitude,
                Longitude = _currentLocation!.Longitude
            });

            if (resp.IsSuccess)
                HasLocationRecord = true;

        }
        // Cố gắng lấy vị trí chính xác, ưu tiên vị trí gần đây nếu có và đủ chính xác, nếu không thì yêu cầu vị trí mới
        private async Task<Location?> GetAccurateLocationAsync()
        {
            try
            {
                var last = await Geolocation.Default.GetLastKnownLocationAsync();

                if (last != null && last.Accuracy <= 100)
                    return last;

                var request = new GeolocationRequest(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(8));

                var loc = await Geolocation.Default.GetLocationAsync(request);

                return loc;
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

                Console.WriteLine($"[GPS] LoadNearby using: {_currentLocation?.Latitude}, {_currentLocation?.Longitude}");
                _currentLocation = await GetAccurateLocationAsync();

                Console.WriteLine($"[GPS] Refresh using: {_currentLocation?.Latitude}, {_currentLocation?.Longitude}");
                if (_currentLocation == null)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không lấy được vị trí.";
                    return;
                }

                var req = new NearbyRequest
                {
                    Latitude = _currentLocation!.Latitude,
                    Longitude = _currentLocation.Longitude,
                    DistanceKm = Distance,
                    Page = _page,
                    PageSize = _pageSize,
                    Gender = SelectedGender
                };

                var resp = await _nearbyService.GetNearbyUsersAsync(req);

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

                    _noMoreData = true;
                    return;
                }

                foreach (var item in data)
                    Nearby.Add(item);

                _page++;

                _lastLoadTime = DateTime.UtcNow;

                UiState = UiViewState.Success;
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
                await _nearbyService.SetLocationVisibilityAsync(false);
                _currentLocation = null;
                Nearby.Clear();
            }                   
            catch (Exception ex)
            {
                await _anv.DisplayAlertAsync("Lỗi", ex.Message);
            }
        }
        // Command để load thêm dữ liệu nearby khi cuộn đến cuối danh sách

        [RelayCommand]
        public Task LoadMoreNearby() => LoadNearby();
        // Command để refresh lại danh sách nearby, reset paging và trạng thái
        [RelayCommand]
        public async Task RefreshNearby()
        {
            IsRefreshing = true;

            try
            {
                _currentLocation = await GetAccurateLocationAsync();
                Console.WriteLine($"[GPS] LoadNearby using: {_currentLocation?.Latitude}, {_currentLocation?.Longitude}");
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
                    await _nearbyService.UpdateLocationAsync(new UpdateLocationRequest
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
            var param = new FilterParam
            {
                OnApply = (gender, dist, locationEnabled) =>
                {
                    SelectedGender = gender;
                    Distance = dist;
                    IsLocationEnabled = locationEnabled;
                }
            };

            await _nv.PushModalAsync<FilterPage, FilterParam>(param);
        }
        // Phương thức để ép tắt location switch nếu có lỗi xảy ra, đảm bảo không bị kẹt ở trạng thái bật mà không hoạt động được
    }
}