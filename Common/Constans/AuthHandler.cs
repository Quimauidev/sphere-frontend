using Sphere.Common.Helpers;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.Common.Constans
{
    internal class AuthHandler : DelegatingHandler
    {
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthHandler(IRefreshTokenService refreshTokenService)
        {
            _refreshTokenService = refreshTokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Nếu cần, làm mới token
            var expiresAt = PreferencesHelper.GetAuthTokenExpiresAt();
            if (expiresAt != null && (expiresAt.Value - DateTime.UtcNow).TotalMinutes <= 2)
            {
                var refreshResult = await _refreshTokenService.TryRefreshTokenAsync();
                if (!refreshResult.IsSuccess)
                {
                    PreferencesHelper.Logout();
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new
                        {
                            message = "Phiên hết hạn",
                            errors = new[] { new { code = "SessionExpired", description = "Phiên đăng nhập đã hết hạn." } }
                        }), Encoding.UTF8, "application/json")
                    };
                }
            }

            // Gắn token vào header
            var token = PreferencesHelper.GetAuthToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("Socket closed", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new
                        {
                            message = "Kết nối đến máy chủ đã bị đóng. Vui lòng kiểm tra lại mạng hoặc thử lại sau.",
                            errors = new[] { new { code = "SocketClosed", description = "Kết nối đến máy chủ đã bị đóng. Vui lòng kiểm tra lại mạng hoặc thử lại sau." } }
                        }), Encoding.UTF8, "application/json")
                    };
                }
                throw;
            }
        }
    }
}
