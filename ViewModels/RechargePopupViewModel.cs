using Android.Content;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class RechargePopupViewModel : ObservableObject
    {
        private readonly IUserSessionService _userSessionService;
        public string QrImage { get; }
        public decimal PriceValue { get; }   // giá nguyên để copy
        public string PriceText { get; }     // hiển thị VND
        public string TransferNote { get; }
        public string BankInfo { get; } = "Vietcombank";
        public string AccountNumber { get; } = "0281000479532";
        public string AccountName { get; } = "HA VAN QUI";

        private readonly string _qrFileName;

        public RechargePopupViewModel(DiamondModel package, string qrImage, IUserSessionService userSessionService)
        {
            QrImage = qrImage;
            // Chỉ lất tên file, không lấy đường dẫn
            _qrFileName = Path.GetFileName(qrImage);
            PriceValue = package.Price; // giữ raw giá trị để copy
            // Hiển thị VND
            PriceText = $"{package.Price:N0} VND";
            _userSessionService = userSessionService;
            // Nội dung CK
            TransferNote = $"NAP_{package.Coins}_SPHERE_ID{_userSessionService.CurrentUser!.UserDTO!.UserIdNumber}";
        }
        // Copy ngân hàng
        [RelayCommand]
        private async Task CopyBankAsync()
        {
            await Clipboard.SetTextAsync(BankInfo);
            await Shell.Current.DisplayAlert("Đã copy", $"Đã sao chép {BankInfo}.", "OK");
        }
        // Copy STK
        [RelayCommand]
        private async Task CopyAccountNumberAsync()
        {
            await Clipboard.SetTextAsync(AccountNumber);
            await Shell.Current.DisplayAlert("Đã copy", $"Đã sao chép {AccountNumber}.", "OK");
        }
        // Copy tên người nhận
        [RelayCommand]
        private async Task CopyAccountNameAsync()
        {
            string moneyForCopy = PriceValue.ToString("N0");  // format theo hàng nghìn

            await Clipboard.SetTextAsync(moneyForCopy);

            await Shell.Current.DisplayAlert("Đã copy", $"Đã sao chép {moneyForCopy}.", "OK");
        }
        // Copy Nội dung CK
        [RelayCommand]
        private async Task CopyNoteAsync()
        {
            await Clipboard.SetTextAsync(TransferNote);
            await Shell.Current.DisplayAlert("Đã copy", "Đã sao chép nội dung chuyển khoản.", "OK");
        }

        [RelayCommand]
        private async Task SaveQrAsync()
        {
            try
            {
                string fileName = "vietcombank.jpg";

                // 1️⃣ Đọc file từ app package
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                // Kiểm tra API
                if (!OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    await Shell.Current.DisplayAlert(
                        "Không hỗ trợ",
                        "Thiết bị Android dưới 10 không thể lưu vào Download mà không cần quyền.",
                        "OK");
                    return;
                }

                var context = Android.App.Application.Context!;
                var contentResolver = context.ContentResolver!;

                // 2️⃣ Chuẩn bị thông tin file
                var values = new ContentValues();
                values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
                values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "image/jpeg");
                values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Download/");

                // 3️⃣ Tạo file trong MediaStore (thư mục Download)
                var uri = contentResolver.Insert(
                    Android.Provider.MediaStore.Downloads.ExternalContentUri!,
                    values
                );

                if (uri == null)
                {
                    await Shell.Current.DisplayAlert("Lỗi", "Không thể tạo file trong Download.", "OK");
                    return;
                }

                // 4️⃣ Ghi dữ liệu vào file vừa tạo
                using var outStream = contentResolver.OpenOutputStream(uri)!;
                await stream.CopyToAsync(outStream);

                await Shell.Current.DisplayAlert("Thành công",
                    "Ảnh đã được lưu vào thư mục Download.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

    }

}
