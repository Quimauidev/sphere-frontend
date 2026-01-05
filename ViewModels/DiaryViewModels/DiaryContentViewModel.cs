using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Kotlin.Jvm;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Views.Pages;
using System;
using System.Threading.Tasks;

namespace Sphere.ViewModels.DiaryViewModels
{
    public partial class DiaryContentViewModel : ObservableObject
    {
        private readonly IDiaryService _diaryService;
        private readonly IServiceProvider _serviceProvider;
       
        public DiaryContentViewModel(IDiaryService diaryService, IServiceProvider serviceProvider, DiaryModel model)
        {
            _diaryService = diaryService;
            _serviceProvider = serviceProvider;
            Model = model;
            IsOwnMess = Model.IsOwNer;
        }

        public DiaryModel Model { get; private set; }
        public DateTime CreatedAt => Model.CreatedAt;

        public string? Content => Model.Content;

        public List<DiaryImageDTO>? Images => Model.Images ?? [];

        public double ImageItemHeight => Model.ImageItemHeight;
        public double ImageItemWidth => Model.ImageItemWidth;

        public bool HasImages => Model.HasImages;
        [ObservableProperty]
        private bool isLoading;


        [ObservableProperty]
        private bool isOwnMess;

        [RelayCommand]
        public async Task ShowMenuAsync()
        {
            if (Model == null) return;
            if (IsOwnMess)
            {
                string action = await Application.Current!.MainPage!.DisplayActionSheet("Tùy chọn bài viết", "Hủy", null, "Sửa", "Xóa");
                if (string.IsNullOrEmpty(action) || action == "Hủy") return;
                if (action == "Sửa") await EditDiary();
                else if (action == "Xóa") await DeleteDiary();
            }
            else
            {
                string action = await Application.Current!.MainPage!.DisplayActionSheet("Tùy chọn bài viết", "Hủy", null, "Tố cáo");
                if (string.IsNullOrEmpty(action) || action == "Hủy") return;
                if (action == "Tố cáo") await ReportDiary();
            }
        }
        [RelayCommand]
        private async Task EditDiary()
        {
            if (Model == null) return;
            var page = _serviceProvider.GetRequiredService<PostDiaryPage>();
            if (page.BindingContext is PostDiaryViewModel vm)
            {
                await vm.LoadForEditAsync(Model);
            }
            await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
        }

        [RelayCommand]
        private async Task DeleteDiary()
        {

            if (IsLoading) return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();
            try
            {
                bool confirm = await Application.Current!.MainPage! .DisplayAlert("Xác nhận", "Bạn có chắc muốn xóa bài viết này?", "Xóa", "Hủy");

                if (!confirm)
                    return;

                var response = await _diaryService.DeleteDiaryAsync(Model.Id);

                if (response.IsSuccess)
                {
                    WeakReferenceMessenger.Default.Send( new DiaryDeletedMessage(Model.Id) );

                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response, "Xóa thất bại");
                }
            }
            finally
            {
                IsLoading = false;
                await PopupHelper.HideLoadingAsync();
            }

        }

        [RelayCommand]
        private async Task ReportDiary()
        {
            await Application.Current!.MainPage!.DisplayAlert("Tố cáo", "Chức năng tố cáo sẽ được phát triển sau.", "OK");
        }
    }
}
