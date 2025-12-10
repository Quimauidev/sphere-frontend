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

        [ObservableProperty]
        public partial ObservableCollection<DiaryModel> Diaries { get; set; } = new ObservableCollection<DiaryModel>();


        [ObservableProperty]
        public partial UiViewState DiaryState { get; set; }

        [ObservableProperty]
        public bool isDiaryLoading;
       

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

        public DiaryListViewModel(IDiaryService diaryService)
        {
            _diaryService = diaryService;
            // Lắng nghe message:
            WeakReferenceMessenger.Default.Register<ReloadDiariesMessage>(this, async (r, m) =>
            {
                await LoadDiaries(forceReload: true);
            });
        }

        [RelayCommand]
        public async Task LoadFirstPage()
        {
            _currentPage = 1;
            Diaries.Clear();
            await LoadDiaries(forceReload: true);
        }

        public async Task LoadDiaries(bool forceReload = false)
        {
            if (IsDiaryLoading || (HasNoMoreData && !forceReload)) return;

            IsDiaryLoading = true;

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
                        Diaries.Add(item);

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
                IsDiaryLoading = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreDiaries()
        {
            await LoadDiaries();
        }
    }
}