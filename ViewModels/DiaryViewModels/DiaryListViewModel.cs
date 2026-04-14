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
        
        private readonly IAppNavigationService _anv;
        private readonly IShellNavigationService _nv;
        private readonly ApiResponseHelper _res;
        private Guid? _userId;

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
        private bool hasNoMoreData;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        [ObservableProperty]
        private string footerKey = Guid.NewGuid().ToString();

        public DiaryListViewModel(IDiaryService diaryService,IAppNavigationService anv , IShellNavigationService nv, ApiResponseHelper res)
        {
            _diaryService = diaryService;
            _anv = anv;
            _nv = nv;
            _res = res;
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
                    ErrorMessage = Diaries.Count == 0 ? "Chưa có bài viết nào" : null;

            });     
        }
        [RelayCommand]
        public async Task RetryAsync()
        {
            await LoadDiaries(forceReload: true);
        }
        [RelayCommand]
       
        public async Task LoadFirstPage(Guid? userId = null)
        {
            _userId = userId;

            _currentPage = 1;
            HasNoMoreData = false;
            Diaries.Clear();

            await LoadDiaries(forceReload: true);
        }
        private async Task ReloadFirstPageAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var response = _userId == null ? await _diaryService.GetListDiaryMeAsync(1, PageSize) : await _diaryService.GetListDiaryOtherAsync(_userId.Value, 1, PageSize);

                if (!response.IsSuccess)
                {
                    ErrorMessage = response.Errors?.FirstOrDefault()?.Description ?? response.Message ?? "Có lỗi xảy ra";
                    return;
                }    

                var items = response.Data?.ToList() ?? [];
                Diaries.Clear();
                foreach (var item in items)
                {
                    Diaries.Add(new DiaryContentViewModel( _diaryService, item, _anv, _nv, _res));
                }

                _currentPage = 2;
                HasNoMoreData = items.Count < PageSize;
                //Set trạng thái UI chuẩn
               
                ErrorMessage = Diaries.Count == 0 ? response.Message ?? "Chưa có bài viết nào" : null;
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
            }

            try
            {

                var response = _userId == null ? await _diaryService.GetListDiaryMeAsync(_currentPage, PageSize) : await _diaryService.GetListDiaryOtherAsync(_userId.Value, _currentPage, PageSize);
                if (response.IsSuccess)
                {
                    var items = response.Data?.ToList() ?? [];

                    if (forceReload)
                        Diaries.Clear();

                    foreach (var item in items)
                        Diaries.Add(new DiaryContentViewModel(_diaryService,item,_anv,_nv, _res));

                    HasNoMoreData = items.Count < PageSize;
                    _currentPage++;

                    // Set trạng thái UI chuẩn
                    ErrorMessage = Diaries.Count == 0 ? response.Message ?? "Chưa có bài viết nào" : null;
                }
                else
                {
                    ErrorMessage = response.Errors?.FirstOrDefault()?.Description ?? response.Message ?? "Có lỗi xảy ra";
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