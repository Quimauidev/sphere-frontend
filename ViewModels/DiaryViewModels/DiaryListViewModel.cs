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
    public partial class DiaryListViewModel : BaseViewModel
    {
        private readonly IDiaryService _diaryService;
        
        private readonly IAppNavigationService _anv;
        private readonly IShellNavigationService _nv;
        private readonly ApiResponseHelper _res;
        private Guid? _userId;

        [ObservableProperty]
        public partial ObservableCollection<DiaryContentViewModel> Diaries { get; set; } = new ObservableCollection<DiaryContentViewModel>();

        [ObservableProperty]
        public bool isLoading;
       
        private int page = 1;
        private int pageSize = 20;
        //public bool IsHomeContext { get; set; }  // true nếu hiển thị trên Home
        //public bool IsPersonalContext => !IsHomeContext; // hiển thị ở trang cá nhân

        public bool HasAnyData => Diaries.Count > 0;
      
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        [ObservableProperty]
        private string footerKey = Guid.NewGuid().ToString();

        public DiaryListViewModel(IDiaryService diaryService,IAppNavigationService anv , IShellNavigationService nv, ApiResponseHelper res)
        {
            _diaryService = diaryService;
            _anv = anv;
            _nv = nv;
            _res = res;
            _ = LoadDiaries();
            WeakReferenceMessenger.Default.Register<DiaryPostedMessage>(this, async (r, m) =>
            {
                await LoadDiaries();
            });
            WeakReferenceMessenger.Default.Register<DiaryUpdatedMessage>(this, async (r, m) =>
            {
                await LoadDiaries();
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
        public async Task ReloadDiary(Guid? userId = null)
        {
            _userId = userId;
            page = 1;
            HasNoMoreData = false;
            Diaries.Clear();

            await LoadDiaries(forceReload: true);
        }

        public async Task LoadDiaries(bool forceReload = false)
        {
            if (IsLoading || (HasNoMoreData && !forceReload)) return;

            IsLoading = true;
            if (forceReload)
            {
                page = 1;
                HasNoMoreData = false;
                Diaries.Clear();
                UiState = UiViewState.Loading;
            }
            try
            {

                var response = _userId == null ? await _diaryService.GetListDiaryMeAsync(page, pageSize) : await _diaryService.GetListDiaryOtherAsync(_userId.Value, page, pageSize);
                if (!response.IsSuccess)
                {
                    UiState = UiViewState.Error;
                    ErrorMessage = response.Errors?.FirstOrDefault()?.Description ?? response.Message ?? "Có lỗi xảy ra";
                    return;
                }
                var data = response.Data ?? [];
                if(page == 1 && !data.Any())
                {
                    UiState = UiViewState.Empty;
                    HasNoMoreData = true;
                    return;
                }
                foreach (var item in data)
                    Diaries.Add(new DiaryContentViewModel(_diaryService, item, _anv, _nv, _res));
                HasNoMoreData = data.Count() < pageSize;
                if (!HasNoMoreData)
                    page++;
                UiState = UiViewState.Success;

            }
            finally
            {
                IsLoading = false;
            }
        }

        [ObservableProperty]
        private bool isLoadingMore;

        [RelayCommand]
        public async Task LoadMoreDiaries()
        {
            if(HasNoMoreData|| IsLoading || IsLoadingMore) return;
            try
            {
                IsLoadingMore = true;
                await LoadDiaries();
            }
            finally
            {
                IsLoadingMore = false;
            }
        }
    }
}