using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Services.IService;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Sphere.Services.Service
{
    internal class ApiService(IHttpClientFactory httpClientFactory) : IApiService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // DELETE Method
        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint, CancellationToken ct = default)
        {
            return SendRequestAsync<object, TResponse>(HttpMethod.Delete, endpoint, null, true, ct);
        }

        // GET Method
        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, CancellationToken ct = default)
        {
            return SendRequestAsync<object, TResponse>(HttpMethod.Get, endpoint, null, true, ct);
        }

        // PATCH Method
        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Patch, endpoint, data, true,ct);
        }

        // PATCH Form
        public Task<ApiResponse<TResponse>> PatchFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default)
        {
            return SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Patch, endpoint, formData, true, ct);
        }

        // POST Method
        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, data, true, ct);
        }

        // POST Form
        public Task<ApiResponse<TResponse>> PostFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default)
        {
            return SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Post, endpoint, formData, true, ct);
        }

        // PUT Method
        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Put, endpoint, data, true, ct);
        }

        // PUT Form
        public Task<ApiResponse<TResponse>> PutFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default)
        {
            return SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Put, endpoint, formData, true,ct);
        }

        private static HttpRequestMessage BuildRequest<TRequest>(HttpMethod method, string endpoint, TRequest? data)
        {
            var request = new HttpRequestMessage(method, endpoint);
            // Xử lý nội dung gửi đi
            if (data != null)
            {
                request.Content = data is MultipartFormDataContent formData
                    ? formData
                    : new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            }
            return request;
        }
        private static bool IsTransient(HttpRequestException ex)
        {
            var msg = ex.Message.ToLower();

            return msg.Contains("timeout") ||
                   msg.Contains("reset") ||
                   msg.Contains("abort") ||
                   msg.Contains("closed");
        }
        // Hàm xử lý chung cho mọi phương thức HTTP
        public Task<ApiResponse<TResponse>> SendRequestAsync<TRequest, TResponse>(HttpMethod method, string endpoint, TRequest? data, bool requireAuth, CancellationToken ct)
        {
            var client = requireAuth ? _httpClientFactory.CreateClient("AuthorizedClient") : _httpClientFactory.CreateClient("PublicClient");
            return SendWithRetryAsync<TRequest, TResponse>(client, method, endpoint, data, ct);
        }
        
        private async Task<ApiResponse<TResponse>> SendWithRetryAsync<TRequest, TResponse>(HttpClient client, HttpMethod method, string endpoint, TRequest? data, CancellationToken ct)
        {
            int retry = 0;
            while (true)
            {
                using var request = BuildRequest(method, endpoint, data);

                try
                {
                    using var response = await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        ct);

                    return await HandleResponse<TResponse>(response);
                }
                catch (HttpRequestException ex) when (retry < 2 && IsTransient(ex))
                {
                    retry++;
                    await Task.Delay(1000, ct);
                }
                catch (TaskCanceledException)
                {
                    return ApiResponse<TResponse>.Fail( "Hết thời gian kết nối", "Timeout", "Kết nối quá lâu hoặc không có internet" );
                }
                catch (HttpRequestException ex)
                {
                    return MapNetworkError<TResponse>(ex);
                }
            }
        }
        private static ApiResponse<T> MapNetworkError<T>(HttpRequestException ex)
        {
            var msg = ex.Message.ToLower();

            if (msg.Contains("abort") || msg.Contains("closed") || msg.Contains("reset"))
            {
                return ApiResponse<T>.Fail( "Kết nối bị gián đoạn", "ConnectionAborted", "Kết nối đến máy chủ bị ngắt giữa chừng" );
            }

            return ApiResponse<T>.Fail( "Lỗi mạng", "NetworkError", "Không thể kết nối đến máy chủ" );
        }
        private async Task<ApiResponse<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                if (result != null)
                    return result;
            }
            catch { }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    Fail<T>(content, "Phiên đăng nhập đã hết hạn"),
                HttpStatusCode.Forbidden =>
                    Fail<T>(content, "Không có quyền truy cập"),
                HttpStatusCode.NotFound =>
                    Fail<T>(content, "Không tìm thấy tài nguyên"),
                var code when (int)code >= 500 =>
                    Fail<T>(content, "Máy chủ đang lỗi"),
                _ =>
                    Fail<T>(content, "Lỗi không xác định")
            };
        }
        private static ApiResponse<T> Fail<T>(string content, string fallback)
        {
            try
            {
                var err = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
                return ApiResponse<T>.Fail(
                    err?.Message ?? fallback,
                    err?.Errors ?? []
                );
            }
            catch
            {
                return ApiResponse<T>.Fail(fallback, "Unknown", fallback);
            }
        }
        public Task<ApiResponse<TResponse>> SendPublicPostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, data, false, ct);
        }
        public Task<ApiResponse<TResponse>> SendRequestAsync<TResponse>(HttpMethod method, string endpoint, CancellationToken ct = default)
        {
            return SendRequestAsync<object, TResponse>(method, endpoint, null, true, ct);
        }

        public Task<ApiResponse<string>> UploadImageAsync(byte[] imageBytes)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            form.Add(fileContent, "file", "upload.jpg");

            // Gọi API — đường dẫn upload là ví dụ, bạn điều chỉnh cho đúng với backend
            return PostFormAsync<string>("/api/uploads/image", form);
        }
    }
}