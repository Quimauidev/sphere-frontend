using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Responses
{
    public static class ApiResponseHelper
    {
        public static async Task ShowApiSuccessAsync<T>(ApiResponse<T> response, string title = "Thành công")
        {
            if (response == null)
            {
                await DisplayAlertSafe("Lỗi", "Không nhận được phản hồi từ máy chủ");
                return;
            }

            // Nếu bạn muốn kiểm tra thêm điều kiện cụ thể (response.Success == true) thì thêm tại đây.
            string message = response.Message ?? "Thao tác thành công";

            await DisplayAlertSafe(title, message);
        }

        public static async Task ShowApiErrorsAsync<T>(ApiResponse<T> response, string title = "Thông báo")
        {
            if (response == null)
            {
                await DisplayAlertSafe("Lỗi", "Không nhận được phản hồi từ máy chủ");
                return;
            }
            // Ưu tiên hiển thị tiếng Việt nếu là lỗi SocketClosed
            var socketClosedError = response.Errors?.FirstOrDefault(e => e.Code == "SocketClosed");
            string message;
            if (socketClosedError != null)
            {
                message = "Kết nối đến máy chủ đã bị đóng. Vui lòng kiểm tra lại mạng hoặc thử lại sau";
            }
            else
            {
                message = response.Errors?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Description))?.Description
                    ?? (!string.IsNullOrWhiteSpace(response.Message) ? response.Message : null)
                    ?? "Thao tác thất bại";
            }
            await DisplayAlertSafe(title, message);
        }

        private static async Task DisplayAlertSafe(string title, string message)
        {
            var page = Application.Current?.MainPage;
            if (page != null)   
            {
                await page.DisplayAlert(title, message, "OK");
            }
        }

        public static async Task ShowAlertAsync(string message)
        => await Application.Current!.MainPage!.DisplayAlert("Thông báo", message, "OK");
    }
}