using Microsoft.Maui.Controls.PlatformConfiguration;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Responses
{
    public class ApiResponseHelper(IAppNavigationService anv)
    {
        private readonly IAppNavigationService _anv = anv;

        public async Task ShowApiSuccessAsync<T>(ApiResponse<T> response, string title = "Thành công")
        {
            if (response == null)
            {
                await _anv.DisplayAlertAsync("Lỗi", "Không nhận được phản hồi từ máy chủ");
                return;
            }

            // Nếu bạn muốn kiểm tra thêm điều kiện cụ thể (response.Success == true) thì thêm tại đây.
            string message = response.Message ?? "Thao tác thành công";

            await _anv.DisplayAlertAsync(title, message);
        }

        public async Task ShowApiErrorsAsync<T>(ApiResponse<T> response, string title = "Thông báo")
        {
            if (response == null)
            {
                await _anv.DisplayAlertAsync("Lỗi", "Không nhận được phản hồi từ máy chủ");
                return;
            }
            // Ưu tiên hiển thị tiếng Việt nếu là lỗi SocketClosed
            var socketClosedError = response.Errors?.FirstOrDefault(e => e.Code == "SocketClosed"|| e.Code == "ConnectionAborted" || e.Code == "NetworkError");
            string message;
            if (socketClosedError != null)
            {
                message = "Kết nối đến máy chủ đã bị đóng hoặc kiểm tra lại mạng hoặc bị gián đoạn. Vui lòng thử lại sau";
            }
            else
            {
                message = response.Errors?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Description))?.Description
                    ?? (!string.IsNullOrWhiteSpace(response.Message) ? response.Message : null)
                    ?? "Thao tác thất bại";
            }
            await _anv.DisplayAlertAsync(title, message);
        }

        public static async Task ShowShellAlertAsync(string title, string message, string cancel = "OK")
            => await Shell.Current.DisplayAlertAsync(title, message, cancel);
        public static async Task<bool> ShowShellConfirmAsync(string title, string message, string accept, string cancel)
            => await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}