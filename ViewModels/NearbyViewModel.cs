using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

namespace Sphere.ViewModels
{
    public partial class NearbyViewModel : BaseViewModel
    {
        // Biến kiểm soát task hiện tại để tránh chạy song song
        private Task? _currentNearbyTask;
        private readonly INearbyService _nearbyService;
        private readonly IPermissionService _permissionService;
        private CancellationTokenSource? _locationCts;
        private DateTime? _lastLoadTime;
        private readonly TimeSpan _minLoadInterval = TimeSpan.FromSeconds(30);
        private readonly IShellNavigationService _nv;
        private readonly IAppNavigationService _anv;
       
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
        private bool _isEnablingLocation;

        [ObservableProperty]
        private ObservableCollection<NearbyModel> nearby = [];

        private bool HasLocationRecord
        {
            get => PreferencesHelper.GetHasLocationRecord();
            set => PreferencesHelper.SetHasLocationRecord(value);
        }

        public NearbyViewModel( INearbyService nearbyService, IPermissionService permissionService, IShellNavigationService nv, IAppNavigationService anv)
        {
            _nearbyService = nearbyService;
            _permissionService = permissionService;

            // 🔹 Đọc trạng thái đã lưu
            IsLocationEnabled = PreferencesHelper.GetLocationEnabled();
            _nv = nv;
            _anv = anv;
        }
        // Yêu cầu cấp quyền vị trí và kiểm tra GPS, trả về true nếu đủ điều kiện.
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

        // Lấy vị trí GPS chính xác, trả về null nếu không lấy được.
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
                 UiState= UiViewState.Loading;
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

            // Đảm bảo chỉ chạy 1 task
            _currentNearbyTask = null;

            if (value)
            {
                _currentNearbyTask = EnableLocationAndLoadNearbyAsync(_locationCts.Token);
            }
            else
            {
                Nearby.Clear();
                _ = DisableLocationAsync();
            }
        }

        private async Task EnableLocationAndLoadNearbyAsync(CancellationToken token)
        {
            if (_isEnablingLocation) return;
            _isEnablingLocation = true;

            try
            {
                
                UiState = UiViewState.Loading;
                IsRefreshing = true;

                var checkGps = await RequestLocationPermissionAndGpsAsync();
                if (!checkGps || token.IsCancellationRequested)
                {
                    if (IsLocationEnabled)
                        IsLocationEnabled = false;
                    ErrorMessage = "Không thể truy cập vị trí. Vui lòng kiểm tra quyền và GPS.";
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
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không lấy được vị trí hiện tại.";
                    return;
                }

                if (!HasLocationRecord)
                {
                    var createResp = await _nearbyService.CreateLocationAsync(new CreateLocationRequest
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    });

                    if (!createResp.IsSuccess)
                    {
                        var errorCode = createResp.Errors?.FirstOrDefault()?.Code;

                        if (errorCode == "LocationExists")
                        {
                            await _nearbyService.SetLocationVisibilityAsync(true);
                            HasLocationRecord = true;
                        }
                        else
                        {
                            UiState = UiViewState.Error;
                            ErrorMessage = createResp.Errors?.FirstOrDefault()?.Description
                                           ?? createResp.Message
                                           ?? "Không thể tạo vị trí.";
                            return;
                        }
                    }
                    else
                    {
                        HasLocationRecord = true;
                    }
                }
                else
                {
                    var visibleResp = await _nearbyService.SetLocationVisibilityAsync(true);

                    if (!visibleResp.IsSuccess)
                    {
                        UiState = UiViewState.Error;
                        ErrorMessage = "Không thể bật chia sẻ vị trí.";
                        return;
                    }
                }

                _lastLoadTime = now;
                if (IsLocationEnabled && !token.IsCancellationRequested)
                    await LoadNearbyInternalAsync(token, forceReload: true); // Chỉ gọi 1 lần, forceReload để đảm bảo đúng trạng thái
            }
            catch
            {
                UiState = UiViewState.Error;
                ErrorMessage = "Đã xảy ra lỗi không xác định.";
            }
            finally
            {
                _isEnablingLocation = false;
                IsRefreshing = false;
            }
        }

        private async Task DisableLocationAsync()
        {
            try
            {
                IsRefreshing = true;
                if (HasLocationRecord)
                {
                    await _nearbyService.SetLocationVisibilityAsync(false);
                }

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
                if (_nearbyLoading || (_nearbyHasNoMoreData && !forceReload))
                    return;

                _nearbyLoading = true;

                if (_nearbyPage == 1 || forceReload)
                    UiState = UiViewState.Loading;

                IsRefreshing = forceReload;

                if (forceReload)
                {
                    _nearbyPage = 1;
                    _nearbyHasNoMoreData = false;
                    Nearby.Clear();
                }

                if (token?.IsCancellationRequested == true || !IsLocationEnabled)
                    return;

                //if (!await RequestLocationPermissionAndGpsAsync())
                //{
                //    UiState = UiViewState.Error;
                //    ErrorMessage = "Không thể truy cập vị trí. Vui lòng kiểm tra quyền và GPS.";
                //    return;
                //}

                var now = DateTime.UtcNow;

                if (!forceReload && _lastLoadTime.HasValue && now - _lastLoadTime.Value < _minLoadInterval)
                {
                    UiState = Nearby.Count > 0
                        ? UiViewState.Success
                        : UiViewState.Empty;
                    return;
                }

                var location = await GetAccurateLocationAsync();

                if (location == null)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "Không lấy được vị trí hiện tại.";
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

                if (!resp.IsSuccess)
                {
                    if (forceReload)
                    {
                        ErrorMessage = resp.Errors?.FirstOrDefault()?.Description ?? resp.Message ?? "Có lỗi xảy ra";
                        UiState = UiViewState.Error;
                    }    
                    return;
                }

                var data = resp.Data ?? [];

                if (data.Any())
                {
                    foreach (var item in data)
                        Nearby.Add(item);

                    _nearbyHasNoMoreData = data.Count() < PageSize;
                    _nearbyPage++;
                    _lastLoadTime = now;

                    UiState = UiViewState.Success;
                }
                else
                {
                    _nearbyHasNoMoreData = true;

                    if (_nearbyPage == 1)
                        UiState = UiViewState.Empty;
                }
            }
            catch
            {
                if (forceReload)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "Đã xảy ra lỗi không xác định.";
                }
            }
            finally
            {
                _nearbyLoading = false;
                IsRefreshing = false;
            }
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
                    // Không gọi LoadNearbyInternalAsync trực tiếp, chỉ để OnIsLocationEnabledChanged xử lý
                }
                else
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = "GPS chưa được bật.";
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
                    UiState = UiViewState.Error;
                    return;
                }
                var location = await GetAccurateLocationAsync();
                if (location == null)
                {
                    UiState = UiViewState.Error;
                    return;
                }
                if (!HasLocationRecord)
                {
                    var createResp = await _nearbyService.CreateLocationAsync(new CreateLocationRequest
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    });

                    if (!createResp.IsSuccess)
                    {
                        UiState = UiViewState.Error;
                        return;
                    }

                    HasLocationRecord = true;
                }
                else
                {
                    var updateResp = await _nearbyService.UpdateLocationAsync(new UpdateLocationRequest
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude
                    });

                    if (!updateResp.IsSuccess)
                    {
                        ErrorMessage = updateResp.Errors?.FirstOrDefault()?.Description ?? updateResp.Message ?? "Có lỗi xảy ra";
                        UiState = UiViewState.Error;
                        return;
                    }
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