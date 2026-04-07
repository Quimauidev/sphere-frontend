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
    internal class AuthHandler(IRefreshTokenService refreshTokenService) : DelegatingHandler
    {
        private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await _refreshLock.WaitAsync(ct);
            try
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
            }
            finally
            {
                _refreshLock.Release();
            }

            // Gắn token vào header
            var token = PreferencesHelper.GetAuthToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, ct);
        }
    }
}
