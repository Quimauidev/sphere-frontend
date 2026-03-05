using Microsoft.Maui.Controls.PlatformConfiguration;
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

        public static async Task DisplayAlertSafe(string title, string message)
        {
            var app = Application.Current;

            if (app?.Windows.Count > 0)
            {
                var page = app.Windows[0].Page;
                if (page != null)
                    await page.DisplayAlertAsync(title, message, "OK");
            }
        }

        public static async Task ShowAlertAsync(string message)
        {
            var app = Application.Current;

            if (app?.Windows.Count > 0)
            {
                var page = app.Windows[0].Page;
                if (page != null)
                {
                    await page.DisplayAlertAsync("Thông báo", message, "OK");
                }
            }
        }
        public static async Task ShowShellAlertAsync(string title, string message)
            => await Shell.Current.DisplayAlertAsync(title, message, "OK");
        public static async Task<bool> ShowShellConfirmAsync(string title, string message, string accept, string cancel)
            => await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}