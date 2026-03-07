using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
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

namespace Sphere.ViewModels
{
    public partial class NearbyViewModel : BaseViewModel
    {
        private readonly INearbyService _nearbyService;
        private readonly IPermissionService _permissionService;
        private CancellationTokenSource? _locationCts;
        private DateTime? _lastLoadTime;
        private readonly TimeSpan _minLoadInterval = TimeSpan.FromSeconds(30);
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;

        [ObservableProperty]
        private UiViewState nearbyState;

        private const int PageSize = 20;
        private int _nearbyPage = 1;

        private bool _nearbyLoading;
        private bool _nearbyHasNoMoreData;

        [ObservableProperty]
        private int distance = 60; // khoảng cách mặc định 60 km

        [ObservableProperty]
        private bool isLocationEnabled;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private Gender? selectedGender;

        [ObservableProperty]
        private ObservableCollection<NearbyModel> nearby = [];

        public NearbyViewModel( INearbyService nearbyService, IPermissionService permissionService, IShellNavigationService nv, IAppNavigationService anv)
        {
            _nearbyService = nearbyService;
            _permissionService = permissionService;

            // 🔹 Đọc trạng thái đã lưu
            IsLocationEnabled = PreferencesHelper.GetLocationEnabled();
            _nv = nv;
            _anv = anv;
        }
        /// <summary>
        /// Yêu cầu cấp quyền vị trí và kiểm tra GPS, trả về true nếu đủ điều kiện.
        /// </summary>
        private async Task<bool> RequestLocationPermissionAndGpsAsync()
        {
            var permissionResult = await _permissionService.RequestPermissionAsync(AppPermission.Location);
            if (permissionResult != PermissionResult.Granted)
                return false;
            if (!_permissionService.IsGpsEnabled())
            {
                await _permissionService.ShowGpsDialogAsync();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Lấy vị trí GPS chính xác, trả về null nếu không lấy được.
        /// </summary>
        private async Task<Location?> GetAccurateLocationAsync(int samples = 3, int maxAccuracyMeters = 80)
        {
            var last = await Geolocation.Default.GetLastKnownLocationAsync();
            if (last != null && last.Accuracy <= maxAccuracyMeters)
                return last;
            var locs = new List<Location>();
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(8));
            for (int i = 0; i < samples; i++)
            {
                if (!_permissionService.IsGpsEnabled())
                    return null;
                try
                {
                    var loc = await Geolocation.Default.GetLocationAsync(request);
                    if (loc != null && loc.Accuracy <= maxAccuracyMeters)
                        locs.Add(loc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi lấy GPS: {ex.Message}");
                }
                await Task.Delay(700);
            }
            if (locs.Count == 0)
                return null;
            return locs.OrderBy(l => l.Accuracy).First();
        }
        public async Task InitAsync()
        {
            if (IsLocationEnabled)
            {
                NearbyState = UiViewState.Loading;
                _locationCts = new CancellationTokenSource();
                await EnableLocationAndLoadNearbyAsync(_locationCts.Token);
            }
        }


        [RelayCommand]
        public async Task LoadFirstNearby()
        {
            _nearbyPage = 1;
            Nearby.Clear();

            await LoadNearbyInternalAsync(forceReload: true);
        }

        partial void OnIsLocationEnabledChanged(bool value)
        {
            PreferencesHelper.SetLocationEnabled(value);

            // Hủy task cũ nếu có
            _locationCts?.Cancel();
            _locationCts?.Dispose();
            _locationCts = new CancellationTokenSource();

            if (value)
            {
                _ = EnableLocationAndLoadNearbyAsync(_locationCts.Token);
            }    
            else
            {
                _locationCts?.Cancel();
                Nearby.Clear();
                _ = DisableLocationAsync();
            }
        }

        private async Task EnableLocationAndLoadNearbyAsync(CancellationToken token)
        {
            try
            {
                NearbyState = UiViewState.Loading;
                IsRefreshing = true;
                if (!await RequestLocationPermissionAndGpsAsync() || token.IsCancellationRequested || !IsLocationEnabled)
                {
                    if (IsLocationEnabled)
                        IsLocationEnabled = false;
                    return;
                }
                var now = DateTime.UtcNow;
                if (_lastLoadTime.HasValue && now - _lastLoadTime.Value < _minLoadInterval)
                {
                    await LoadNearbyInternalAsync(token);
                    return;
                }
                var location = await GetAccurateLocationAsync();
                if (location == null || token.IsCancellationRequested || !IsLocationEnabled)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                var updateReq = new UpdateLocationRequest
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    IsVisible = true
                };
                var updateResp = await _nearbyService.UpdateLocationAsync(updateReq);
                if (!updateResp.IsSuccess || token.IsCancellationRequested || !IsLocationEnabled)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                _lastLoadTime = now;
                if (IsLocationEnabled && !token.IsCancellationRequested)
                    await LoadNearbyInternalAsync(token);
            }
            catch
            {
                NearbyState = UiViewState.Error;
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task DisableLocationAsync()
        {
            try
            {
                IsRefreshing = true;

                // 🔹 Gửi API để ẩn vị trí
                var updateReq = new UpdateLocationRequest
                {
                    IsVisible = false
                };

                var resp = await _nearbyService.UpdateLocationAsync(updateReq);

                // 🔹 Xóa danh sách nearby
                Nearby.Clear();
            }
            catch (Exception ex)
            {
                await _anv.DisplayAlertAsync("Lỗi",$"Đã có lỗi xảy ra khi tắt chia sẻ vị trí: {ex.Message}. Vui lòng thử lại.");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task LoadNearbyInternalAsync(CancellationToken? token = default, bool forceReload = false)
        {
            try
            {
                if (_nearbyLoading || (_nearbyHasNoMoreData && !forceReload)) return;
                _nearbyLoading = true;
                IsRefreshing = forceReload;
                if (forceReload)
                {
                    _nearbyPage = 1;
                    _nearbyHasNoMoreData = false;
                    Nearby.Clear();
                    NearbyState = UiViewState.Loading;
                }

                if (token?.IsCancellationRequested == true || !IsLocationEnabled)
                    return;

                if (!await RequestLocationPermissionAndGpsAsync() || token?.IsCancellationRequested == true || !IsLocationEnabled)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                // --- Giới hạn gọi API theo thời gian ---
                var now = DateTime.UtcNow;
                if (!forceReload && _lastLoadTime.HasValue && now - _lastLoadTime.Value < _minLoadInterval)
                {
                    // Quá gần lần load trước, chỉ return
                    return;
                }

                var location = await GetAccurateLocationAsync();
                if (location == null || token?.IsCancellationRequested == true || !IsLocationEnabled)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }

                var req = new NearbyRequest
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    DistanceKm = Distance,
                    Page = _nearbyPage,
                    PageSize = PageSize,
                    Gender = SelectedGender
                };

                var resp = await _nearbyService.GetNearbyUsersAsync(req);
                if (token?.IsCancellationRequested == true || !IsLocationEnabled)
                    return;

                if (!resp.IsSuccess)
                {
                    // Nếu đang load thêm → không đổi trạng thái toàn trang
                    if (forceReload)
                    {
                        Nearby.Clear();
                        NearbyState = UiViewState.Error;
                    }    
                    return;
                }

                var data = resp.Data ?? [];
                
                if (forceReload) Nearby.Clear();
                if (data.Any())
                {
                    foreach (var item in data)
                        Nearby.Add(item);

                    _nearbyHasNoMoreData = data.Count() < PageSize;
                    _nearbyPage++;
                    _lastLoadTime = now; // lưu thời điểm lần cuối load Nearby
                    NearbyState = UiViewState.Success;
                }
                else
                {
                    _nearbyHasNoMoreData = true;
                    if (_nearbyPage == 1)
                        NearbyState = UiViewState.Empty;
                }
            }
            catch
            {
                if (forceReload)
                    NearbyState = UiViewState.Error;
            }
            finally
            {
                _nearbyLoading = false;
                IsRefreshing = false;
            }
        }

        private async Task<Location?> GetStableLocationAsync(int samples = 3, int maxAccuracyMeters = 80)
        {
            // thử lấy location cache trước
            var last = await Geolocation.Default.GetLastKnownLocationAsync();
            if (last != null && last.Accuracy <= maxAccuracyMeters)
                return last;
            var locs = new List<Location>();
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(8));

            for (int i = 0; i < samples; i++)
            {
                if (!_permissionService.IsGpsEnabled())
                    return null;
                try
                {
                    var loc = await Geolocation.Default.GetLocationAsync(request);
                    if (loc != null && loc.Accuracy <= maxAccuracyMeters)
                        locs.Add(loc);


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi lấy GPS: {ex.Message}");
                }
                await Task.Delay(700); // đợi GPS fix lại
            }

            if (locs.Count == 0)
                return null;

            // Lấy trung bình để giảm sai số
            //double avgLat = locs.Average(l => l.Latitude);
            //double avgLon = locs.Average(l => l.Longitude);
            //return new Location(avgLat, avgLon);
            return locs.OrderBy(l => l.Accuracy).First();// chọn location chính xác nhất
        }

        private bool _isCheckingGps;

        public async Task CheckGpsAfterSettingsAsync()
        {
            if (_isCheckingGps) return;
            _isCheckingGps = true;

            try
            {
                var permissionResult = await _permissionService.RequestPermissionAsync(AppPermission.Location);
                bool gpsEnabled = await _permissionService.CheckGpsStatusAsync();
                if (permissionResult == PermissionResult.Granted && gpsEnabled)
                {
                    // ✅ Nếu quyền OK và GPS bật → bật switch
                    if (!IsLocationEnabled)
                        IsLocationEnabled = true;
                    _locationCts?.Cancel();
                    _locationCts?.Dispose();
                    _locationCts = new CancellationTokenSource();
                    await LoadNearbyInternalAsync(_locationCts.Token);
                }
                else
                {
                    // ❌ Nếu permission/GPS không OK → tắt switch
                    if (IsLocationEnabled)
                        IsLocationEnabled = false;
                }
            }
            finally
            {
                _isCheckingGps = false;
            }
        }

        [RelayCommand]
        public Task LoadMoreNearby() => LoadNearbyInternalAsync();

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

        [RelayCommand]
        public async Task RefreshNearby()
        {
            IsRefreshing = true;
            try
            {
                // Lấy vị trí mới
                if (!await RequestLocationPermissionAndGpsAsync() || !IsLocationEnabled)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                var location = await GetAccurateLocationAsync();
                if (location == null)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                // Update vị trí lên server
                var updateReq = new UpdateLocationRequest
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    IsVisible = true
                };
                var updateResp = await _nearbyService.UpdateLocationAsync(updateReq);
                if (!updateResp.IsSuccess)
                {
                    NearbyState = UiViewState.Error;
                    return;
                }
                // Load lại danh sách nearby
                _nearbyPage = 1;
                Nearby.Clear();
                await LoadNearbyInternalAsync(forceReload: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        public void ForceDisableLocationSwitch()
        {
            if (IsLocationEnabled)
                IsLocationEnabled = false;
        }
    }
}