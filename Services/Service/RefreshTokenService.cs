using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Services.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class RefreshTokenService(IHttpClientFactory httpClientFactory) : IRefreshTokenService
    {
        public static string BaseUrl = "https://sphere-iqm8.onrender.com";
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
       

        public async Task<ApiResponse<bool>> TryRefreshTokenAsync()
        {
            var refreshToken = PreferencesHelper.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return ApiResponse<bool>.Fail("Không tồn tại.", [new ErrorDetail { Code = "NoContent", Description = "Refresh token không tồn tại" }]);
            }

            var json = JsonSerializer.Serialize(new { RefreshToken = refreshToken });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            var response = await client.PostAsync("/api/refresh-token", content);
            var body = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return ApiResponse<bool>.Fail("Không nhận được phản hồi từ máy chủ",
                    [new ErrorDetail { Code = "EmptyResponse", Description = "Phản hồi trống từ máy chủ" }]);
            }

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null || !result.IsSuccess || result.Data == null)
                {
                    return ApiResponse<bool>.Fail("Refresh token không hợp lệ hoặc đã hết hạn",
                        [new ErrorDetail { Code = "InvalidToken", Description = "Refresh token không hợp lệ hoặc đã hết hạn" }]);
                }

                PreferencesHelper.SetAuthToken(result.Data.Token);
                PreferencesHelper.SetRefreshToken(result.Data.RefreshToken);
                PreferencesHelper.SetAuthTokenExpiresAt(DateTime.UtcNow.AddSeconds(result.Data.ExpiresIn));

                return ApiResponse<bool>.Success("Refresh thành công", true);
            }
            catch (JsonException)
            {
                return ApiResponse<bool>.Fail("Phản hồi refresh token không hợp lệ",
                    [new ErrorDetail { Code = "DeserializeError", Description = "Lỗi parse phản hồi từ máy chủ" }]);
            }
        }
    }
}
