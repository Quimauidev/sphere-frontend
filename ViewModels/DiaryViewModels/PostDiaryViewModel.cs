using Android.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Interfaces;
using Sphere.Models;
using Sphere.Models.Params;
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
    public partial class PostDiaryViewModel : ObservableObject, IModalParameterReceiver<EditDiaryNavigationParam>
    {
        private readonly IDiaryService _diaryService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediaUploadService _mediaUploadService;
        private readonly IShellNavigationService _nv;
        public PostDiaryViewModel(IDiaryService diaryService, IServiceProvider serviceProvider, IMediaUploadService mediaUploadService, IShellNavigationService nv)
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
            _nv = nv;
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

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private Guid? editingDiaryId;

        private DiaryModel? _originalDiary;
        public string PostButtonText => IsEditMode ? "Lưu" : "Đăng";

        partial void OnIsEditModeChanged(bool value)
        {
            OnPropertyChanged(nameof(PostButtonText));
        }

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

        public Task LoadForEditAsync(DiaryModel diary)
        {
            IsEditMode = true;
            EditingDiaryId = diary.Id;
            _originalDiary = diary;
            PostDiaryModel.Content = diary.Content;
            PostDiaryModel.ImagePaths = new ObservableCollection<string>(diary.Images?.Select(img => img.Url) ?? []);
            SelectedPrivacy = diary.Privacy;
            RefreshGalleryItems();
            OnPropertyChanged(nameof(PostDiaryModel));
            return Task.CompletedTask;
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
            await _nv.PushModalAsync<ImagePickerPage, ImagePickerNavigationParam>( new ImagePickerNavigationParam { AlreadyPickedCount = PostDiaryModel.ImagePaths.Count });
        }

        [RelayCommand]
        private async Task SaveEditDiaryAsync()
        {
            if (_originalDiary == null || EditingDiaryId == null)
                return;
            // 1️ Ảnh cũ bị xóa → lấy ID
            var removeImageIds = _originalDiary.Images!
                .Where(img => !PostDiaryModel.ImagePaths.Contains(img.Url))
                .Select(img => img.Id)
                .ToList();


            // 2️ Ảnh mới local cần upload
            var newImagePaths = PostDiaryModel.ImagePaths
                .Where(p => !p.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Kiểm tra thay đổi nội dung và quyền riêng tư
            bool isContentChanged = PostDiaryModel.Content != _originalDiary.Content;
            bool isPrivacyChanged = SelectedPrivacy != _originalDiary.Privacy;
            bool isImagesChanged = removeImageIds.Any() || newImagePaths.Any();

            if (!isContentChanged && !isPrivacyChanged && !isImagesChanged)
            {
                await ApiResponseHelper.ShowAlertAsync("Bạn chưa thay đổi dữ liệu mới nào");
                return;
            }
            // 4️ Không cho lưu nếu cả nội dung + ảnh đều trống
            bool hasAnyImage = (_originalDiary.Images!.Count - removeImageIds.Count) > 0 || newImagePaths.Any();
            // 4 Kiểm tra: nếu cả nội dung và ảnh đều trống
            if (string.IsNullOrWhiteSpace(PostDiaryModel.Content) && !hasAnyImage)
            {
                await ApiResponseHelper.ShowAlertAsync("Nội dung hoặc ảnh không được để trống");
                return;
            }
            if(IsLoading) return;
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();
            try
            {
                //gọi service
                var response = await _diaryService.PatchFormDiaryByIdAsync(EditingDiaryId.Value, PostDiaryModel.Content,SelectedPrivacy, removeImageIds, newImagePaths);
                if (response.IsSuccess && response.Data != null)
                {
                    // Gửi message cập nhật diary
                    WeakReferenceMessenger.Default.Send(new DiaryUpdatedMessage(response.Data));

                    await Shell.Current.Navigation.PopModalAsync();
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(response);
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PostDiaryAsync()
        {
            if (IsEditMode)
            {
                await SaveEditDiaryAsync();
                return;
            }

            if (!await ValidatePostDiary())
                return;
           
            IsLoading = true;
            await PopupHelper.ShowLoadingAsync();
            try
            {
                PostDiaryModel.Privacy = SelectedPrivacy;
                var imageUrls = await _mediaUploadService.ResizeAndUploadImagesAsync(PostDiaryModel.ImagePaths.ToList());
                PostDiaryModel.ImagePaths = new ObservableCollection<string>(imageUrls);
                var response = await _diaryService.CreateDiaryAsync(PostDiaryModel);
                if(response.IsSuccess && response.Data != null)
                {
                    
                    WeakReferenceMessenger.Default.Send(new DiaryPostedMessage(true));
                    PostDiaryModel.Content = null;
                    PostDiaryModel.ImagePaths.Clear();
                    SelectedPrivacy = Privacy.Public;
                    OnPropertyChanged(nameof(CanAddMoreImages));
                    OnPropertyChanged(nameof(PostDiaryModel));
                    await _nv.PopModalAsync();
                }
            }
            finally
            {
                await PopupHelper.HideLoadingAsync();
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

        private async Task<bool> ValidatePostDiary()
        {
            if (string.IsNullOrWhiteSpace(PostDiaryModel.Content) && PostDiaryModel.ImagePaths.Count == 0)
            {
                await ApiResponseHelper.ShowAlertAsync("Vui lòng nhập nội dung hoặc chọn ít nhất 1 ảnh");
                return false;
            }
            return true;
        }

        public async Task Receive(EditDiaryNavigationParam parameter)
        {
            await LoadForEditAsync(parameter.Diary);
        }
    }
}