using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Sphere.Common.Responses;
using Sphere.Interfaces;
using Sphere.Models;
using Sphere.Models.Params;
using Sphere.Platforms.Android;
using Sphere.Reloads;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class ImagePickerViewModel : ObservableObject, IModalParameterReceiver<ImagePickerNavigationParam>
    {
        private readonly MediaStoreHelper _mediaStoreHelper;
        private readonly IImagePickerService _imagePickerService;
        private readonly IShellNavigationService _nv;

        [ObservableProperty]
        private ObservableCollection<ImageItem> allImages = [];

        private readonly int limit = 12;
        private int offset = 0;

        [ObservableProperty]
        private ObservableCollection<ImageItem> selectedImages = [];

        public ImagePickerViewModel(MediaStoreHelper mediaStoreHelper, IImagePickerService imagePickerService, IShellNavigationService nv)
        {
            _mediaStoreHelper = mediaStoreHelper;
            _imagePickerService = imagePickerService;
            _ = LoadImages();
            _nv = nv;
        }

        public int AlreadyPickedCount { get; set; } = 0;
        public int MaxPickCount { get; set; } = 9;
        public int RemainingPickCount => MaxPickCount - AlreadyPickedCount - SelectedImages.Count;

        [RelayCommand]
        public async Task LoadImages()
        {
            var newImages = await _mediaStoreHelper.GetImageUrisAsync(limit, offset);

            foreach (var uri in newImages)
                AllImages.Add(new ImageItem { ContentUriString = uri.ToString() });
            offset += limit;
        }

        [RelayCommand]
        public async Task LoadMore()
        {
            await LoadImages();
        }

        [RelayCommand]
        private async Task Done()
        {
            var finalPaths = new List<string>();
            foreach (var img in SelectedImages)
            {
                var uri = Android.Net.Uri.Parse(img.ContentUriString!);
                var path = await _imagePickerService.CopyContentUriToTempFileAsync(uri!);
                finalPaths.Add(path);
            }

            // Trả SelectedImages về ViewModel gọi picker
            WeakReferenceMessenger.Default.Send(new ImagePickerResultMessage(finalPaths));

            await _nv.PopAsync();
        }

        private void ReorderSelectedImages()
        {
            int order = 1;
            foreach (var item in AllImages.Where(x => x.IsSelected))
            {
                item.OrderNumber = order++;
            }
        }

        private bool _isToggling = false;

        [RelayCommand]
        private async Task ToggleSelection(ImageItem image)
        {
            if (_isToggling) return;
            _isToggling = true;
            try
            {
                if (image.IsSelected)
                {
                    image.IsSelected = false;
                    SelectedImages.Remove(image);
                    Reorder();

                    ReorderSelectedImages(); // Cập nhật lại số thứ tự cho ảnh còn lại
                }
                else
                {
                    if (AlreadyPickedCount + SelectedImages.Count >= MaxPickCount)
                    {
                        await ApiResponseHelper.ShowShellAlertAsync("Thông báo", $"Chỉ được chọn tối đa {MaxPickCount} ảnh");
                        return;
                    }
                    image.IsSelected = true;
                    SelectedImages.Add(image);
                    image.OrderNumber = SelectedImages.Count;
                }
            }
            finally
            {
                _isToggling = false;
            }
        }

        private void Reorder()
        {
            int order = 1;
            foreach (var img in AllImages.Where(x => x.IsSelected))
                img.OrderNumber = order++;
        }

        public async Task Receive(ImagePickerNavigationParam param)
        {
            AlreadyPickedCount = param.AlreadyPickedCount;
        }
    }
}