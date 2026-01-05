using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels.DiaryViewModels
{
    public partial class DiaryListViewModel : ObservableObject
    {
        private readonly IDiaryService _diaryService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        public partial ObservableCollection<DiaryContentViewModel> Diaries { get; set; } = new ObservableCollection<DiaryContentViewModel>();


        [ObservableProperty]
        public partial UiViewState DiaryState { get; set; }

        [ObservableProperty]
        public bool isLoading;
       

        private int _currentPage = 1;
        private const int PageSize = 20;
        //public bool IsHomeContext { get; set; }  // true nếu hiển thị trên Home
        //public bool IsPersonalContext => !IsHomeContext; // hiển thị ở trang cá nhân

        public bool HasAnyData => Diaries.Count > 0;

        [ObservableProperty]
        public partial string? ErrorMessage { get; set; }

        [ObservableProperty]
        private bool hasNoMoreData; // đúng

        [ObservableProperty]
        private string footerKey = Guid.NewGuid().ToString();

        public DiaryListViewModel(IDiaryService diaryService, IServiceProvider serviceProvider)
        {
            _diaryService = diaryService;
            _serviceProvider = serviceProvider;

            WeakReferenceMessenger.Default.Register<DiaryPostedMessage>(this, async (r, m) =>
            {
                await ReloadFirstPageAsync();
            });
            WeakReferenceMessenger.Default.Register<DiaryUpdatedMessage>(this, async (r, m) =>
            {
                await ReloadFirstPageAsync();
            });

            //Không reload, Không reset page, Không nhảy scroll, Xóa liên tiếp mượt như Facebook
            WeakReferenceMessenger.Default.Register<DiaryDeletedMessage>(this, (r, msg) =>
            {
                var item = Diaries.FirstOrDefault(d => d.Model.Id == msg.DiaryId);
                if (item != null)
                {
                    Diaries.Remove(item);
                }
                if (Diaries.Count == 0)
                    DiaryState = UiViewState.Empty;
            });
        }

        [RelayCommand]
        public async Task LoadFirstPage()
        {
            _currentPage = 1;
            Diaries.Clear();
            await LoadDiaries(forceReload: true);
        }
        private async Task ReloadFirstPageAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var response = await _diaryService.GetListDiaryAsync(1, PageSize);
                if (!response.IsSuccess)
                    return;

                var items = response.Data?.ToList() ?? [];
                Diaries.Clear();
                if (items.Count == 0)
                {
                    HasNoMoreData = true;
                    return;
                }

                foreach (var item in items)
                {
                    Diaries.Add(new DiaryContentViewModel( _diaryService, _serviceProvider, item));
                }

                _currentPage = 2;
                HasNoMoreData = items.Count < PageSize;
            }
            finally
            {
                IsLoading = false;
            }
        }
        public async Task LoadDiaries(bool forceReload = false)
        {
            if (IsLoading || (HasNoMoreData && !forceReload)) return;

            IsLoading = true;

            if (forceReload)
            {
                _currentPage = 1;
                HasNoMoreData = false;
                Diaries.Clear();
                DiaryState = UiViewState.Loading; // chỉ lần đầu
            }

            try
            {

                var response = await _diaryService.GetListDiaryAsync(_currentPage, PageSize);
                if (response.IsSuccess)
                {
                    var items = response.Data?.ToList() ?? [];

                    if (forceReload)
                        Diaries.Clear();

                    foreach (var item in items)
                        Diaries.Add(new DiaryContentViewModel(_diaryService,_serviceProvider,item));

                    HasNoMoreData = items.Count < PageSize;
                    _currentPage++;

                    // Set trạng thái UI chuẩn
                    if (Diaries.Count == 0)
                    {
                        DiaryState = UiViewState.Empty;
                        ErrorMessage = response.Message;
                    }
                    else
                    {
                        DiaryState = UiViewState.Success;
                        ErrorMessage = null;
                    }
                }
                else
                {
                    // Cách 2: gán ErrorMessage trước, set state sau
                    var msg = response.Errors?.FirstOrDefault()?.Description
                              ?? response.Message
                              ?? "Có lỗi xảy ra";
                    ErrorMessage = msg;

                    if (response.Errors?.Any(e => e.Code is "NetworkError" or "Timeout" or "UnhandledException") == true)
                        DiaryState = UiViewState.Offline;
                    else
                        DiaryState = UiViewState.Error;
                }

                FooterKey = Guid.NewGuid().ToString(); // ép render lại Footer
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreDiaries()
        {
            await LoadDiaries();
        }
    }
}