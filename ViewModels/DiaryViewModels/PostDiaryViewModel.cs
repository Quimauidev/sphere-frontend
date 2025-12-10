using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Reloads;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Sphere.ViewModels
{
    internal partial class PostDiaryViewModel : ObservableObject
    {
        private readonly IDiaryService _diaryService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediaUploadService _mediaUploadService;
        public PostDiaryViewModel(IDiaryService diaryService, IServiceProvider serviceProvider, IMediaUploadService mediaUploadService)
        {
            _diaryService = diaryService;
            _serviceProvider = serviceProvider;
            _mediaUploadService = mediaUploadService;
            PostDiaryModel = new PostDiaryModel
            {
                Privacy = Privacy.Public
            };
            PostDiaryModel.ImagePaths.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CanAddMoreImages));
            };

            RefreshGalleryItems();
            
        }

        public bool CanAddMoreImages => PostDiaryModel.ImagePaths.Count < 9;
        public ObservableCollection<string> GalleryItems { get; set; } = [];

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial PostDiaryModel PostDiaryModel { get; set; }

        public List<Privacy> PrivacyOptions { get; } = PrivacyValues.All;

        [ObservableProperty]
        public partial Privacy SelectedPrivacy { get; set; }

        private void OnImagePicked(object recipient, ImagePickerResultMessage message)
        {
            // Xử lý kết quả
            var pickedImages = message.Value;

            foreach (var img in pickedImages)
            {
                if (PostDiaryModel.ImagePaths.Count < 9)
                    PostDiaryModel.ImagePaths.Add(img.ToString()!);
                else
                    break;
            }

            OnPropertyChanged(nameof(CanAddMoreImages));
            RefreshGalleryItems();

            // Hủy đăng ký ngay sau khi xử lý
            WeakReferenceMessenger.Default.Unregister<ImagePickerResultMessage>(this);
        }

        partial void OnPostDiaryModelChanged(PostDiaryModel value)
        {
            OnPropertyChanged(nameof(CanAddMoreImages));
        }

        partial void OnSelectedPrivacyChanged(Privacy value)
        {
            PostDiaryModel.Privacy = value;
        }

        [RelayCommand]
        private async Task OpenImagePicker()
        {
            if (PostDiaryModel.ImagePaths.Count >= 9)
                return;

            // Đăng ký nhận Message
            WeakReferenceMessenger.Default.Unregister<ImagePickerResultMessage>(this);
            WeakReferenceMessenger.Default.Register<ImagePickerResultMessage>(this, OnImagePicked);
            // Gọi page chọn ảnh — Không còn return List
            var page = _serviceProvider.GetRequiredService<ImagePickerPage>();  
            // Ép BindingContext về đúng VM
            if (page.BindingContext is ImagePickerViewModel vm)
            {
                vm.AlreadyPickedCount = PostDiaryModel.ImagePaths.Count;
            }
            await Shell.Current.Navigation.PushModalAsync(page);
        }

        [RelayCommand]
        private async Task PostDiaryAsync()
        {
            if (!ValidatePostDiary()) return;
            IsLoading = true;
            PopupHelper.ShowLoading();
            try
            {
                PostDiaryModel.Privacy = SelectedPrivacy;
                var imageUrls = await _mediaUploadService.ResizeAndUploadImagesAsync(PostDiaryModel.ImagePaths.ToList());
                PostDiaryModel.ImagePaths = new ObservableCollection<string>(imageUrls);
                var response = await _diaryService.CreateDiaryAsync(PostDiaryModel);
                if (response.Data != null && response.IsSuccess)
                {
                    PostDiaryModel.Content = null;
                    PostDiaryModel.ImagePaths.Clear();
                    SelectedPrivacy = Privacy.Public;
                    OnPropertyChanged(nameof(CanAddMoreImages));
                    OnPropertyChanged(nameof(PostDiaryModel));
                    // Gửi message:
                    WeakReferenceMessenger.Default.Send(new ReloadDiariesMessage(true));
                    await Shell.Current.Navigation.PopModalAsync();
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response, "Đăng thất bại");
                }
            }
            finally
            {
                PopupHelper.HideLoading();
                IsLoading = false;
            }
        }

        private void RefreshGalleryItems()
        {
            var targetItems = new List<string>(PostDiaryModel.ImagePaths);
            if (targetItems.Count < 9)
                targetItems.Add("+");

            // So sánh và đồng bộ danh sách theo từng phần tử
            int i = 0;
            while (i < targetItems.Count && i < GalleryItems.Count)
            {
                if (GalleryItems[i] != targetItems[i])
                {
                    GalleryItems[i] = targetItems[i];
                }
                i++;
            }

            // Thêm phần còn thiếu
            for (; i < targetItems.Count; i++)
            {
                GalleryItems.Add(targetItems[i]);
            }

            // Xóa phần dư
            while (GalleryItems.Count > targetItems.Count)
            {
                GalleryItems.RemoveAt(GalleryItems.Count - 1);
            }
        }

        [RelayCommand]
        private void RemoveImage(string imagePath)
        {
            PostDiaryModel.ImagePaths.Remove(imagePath);
            OnPropertyChanged(nameof(CanAddMoreImages));
            OnPropertyChanged(nameof(PostDiaryModel));
            RefreshGalleryItems();
        }

        private bool ValidatePostDiary()
        {
            if (string.IsNullOrWhiteSpace(PostDiaryModel.Content) && PostDiaryModel.ImagePaths.Count == 0)
            {
                Shell.Current.DisplayAlert("Thông báo", "Vui lòng nhập nội dung hoặc chọn ít nhất 1 ảnh", "OK");
                return false;
            }
            return true;
        }
    }
}