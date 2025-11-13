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
        const string BaseUrl = "https://sphere-iqm8.onrender.com";
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        // DELETE Method
        public async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<object, TResponse>(HttpMethod.Delete, endpoint, null);
        }

        // GET Method
        public async Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint)
        {
            return await SendRequestAsync<object, TResponse>(HttpMethod.Get, endpoint, null);
        }

        // PATCH Method
        public async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            return await SendRequestAsync<TRequest, TResponse>(HttpMethod.Patch, endpoint, data);
        }

        // PATCH Form
        public async Task<ApiResponse<TResponse>> PatchFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData)
        {
            return await SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Patch, endpoint, formData);
        }

        // POST Method
        public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            return await SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, data);
        }

        // POST Form
        public async Task<ApiResponse<TResponse>> PostFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData)
        {
            return await SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Post, endpoint, formData);
        }

        // PUT Method
        public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            return await SendRequestAsync<TRequest, TResponse>(HttpMethod.Put, endpoint, data);
        }

        // PUT Form
        public async Task<ApiResponse<TResponse>> PutFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData)
        {
            return await SendRequestAsync<MultipartFormDataContent, TResponse>(HttpMethod.Put, endpoint, formData);
        }

        private static ApiResponse<TResponse> ParseApiError<TResponse>(string content, JsonSerializerOptions options, string fallbackMessage)
        {
            if (string.IsNullOrWhiteSpace(content) || !content.TrimStart().StartsWith('{'))
            {
                // Không phải JSON, trả về fallback
                return ApiResponse<TResponse>.Fail(fallbackMessage, new List<ErrorDetail> { new ErrorDetail { Code = "InvalidResponse", Description = content } });
            }
            try
            {
                var error = JsonSerializer.Deserialize<ApiResponse<object>>(content, options);
                var message = error?.Message ?? fallbackMessage;
                var errors = error?.Errors ?? new List<ErrorDetail> { new ErrorDetail { Code = "Unhandled", Description = message } };
                return ApiResponse<TResponse>.Fail(message, errors);
            }
            catch
            {
                return ApiResponse<TResponse>.Fail(fallbackMessage, new List<ErrorDetail> { new ErrorDetail { Code = "DeserializeError", Description = fallbackMessage } });
            }
        }

        private HttpRequestMessage BuildRequest<TRequest>(HttpMethod method, string endpoint, TRequest? data, bool requireAuth)
        {
            HttpRequestMessage request = new(method, endpoint);
            if(requireAuth)
            {
                // Thêm Authorization nếu có token
                var token = PreferencesHelper.GetAuthToken(); // Lấy token từ Preferences (hoặc SecureStorage)
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
            }    
            
            // Xử lý nội dung gửi đi
            if (data != null)
            {
                request.Content = data is MultipartFormDataContent formData
                    ? formData
                    : new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            }
            return request;
        }

        // Hàm xử lý chung cho mọi phương thức HTTP
        public async Task<ApiResponse<TResponse>> SendRequestAsync<TRequest, TResponse>(HttpMethod method, string endpoint, TRequest? data, bool requireAuth = true)
        {
            try
            {
                var client = requireAuth ? _httpClientFactory.CreateClient("AuthorizedClient") : _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
                return await SendWithRetryAsync<TRequest, TResponse>(client, method, endpoint, data, requireAuth);
            }
            catch (WebException ex)
            {
                var message = ex.Message.ToLowerInvariant();
                if (message.Contains("socket") ||
                    message.Contains("connection reset") ||
                    message.Contains("forcibly closed") ||
                    message.Contains("closed by the remote host"))
                {
                    return ApiResponse<TResponse>.Fail(
                        "Máy chủ tạm thời không phản hồi",
                        new List<ErrorDetail> {
                    new ErrorDetail {
                        Code = "ServerClosed",
                        Description = "Kết nối bị đóng do máy chủ (có thể đang khởi động hoặc tạm ngưng). Vui lòng thử lại sau."
                    }
                        });
                }

                return ApiResponse<TResponse>.Fail(
                    "Lỗi kết nối đến máy chủ",
                    new List<ErrorDetail> {
                new ErrorDetail {
                    Code = "WebException",
                    Description = ex.Message
                }
                    });
            }
            catch (HttpRequestException)
            {
                return ApiResponse<TResponse>.Fail("Lỗi mạng", new List<ErrorDetail> { new ErrorDetail { Code = "NetworkError", Description = "Không có kết nối internet" } });
            }
            catch (TaskCanceledException)
            {
                return ApiResponse<TResponse>.Fail("Hết thời gian kết nối", new List<ErrorDetail> { new ErrorDetail { Code = "Timeout", Description = "Không có kết nối internet hoặc quá thời gian chờ" } });
            }
        }
        private async Task<ApiResponse<TResponse>> SendWithRetryAsync<TRequest, TResponse>( HttpClient client, HttpMethod method, string endpoint, TRequest? data, bool requireAuth)
        {
            var request = BuildRequest(method, endpoint, data, requireAuth);
            // Cấu hình JsonSerializer
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            HttpResponseMessage response;
            try
            {
                // Dùng ResponseHeadersRead để stream lớn cũng đọc được
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (WebException ex)
            {
                var message = ex.Message.ToLowerInvariant();
                if (message.Contains("socket") ||
                    message.Contains("connection reset") ||
                    message.Contains("forcibly closed") ||
                    message.Contains("closed by the remote host"))
                {
                    return ApiResponse<TResponse>.Fail(
                        "Máy chủ tạm thời không phản hồi",
                        new List<ErrorDetail> {
                    new ErrorDetail {
                        Code = "ServerClosed",
                        Description = "Kết nối bị đóng do máy chủ (có thể đang khởi động hoặc tạm ngưng). Vui lòng thử lại sau."
                    }
                        });
                }

                return ApiResponse<TResponse>.Fail(
                    "Lỗi kết nối đến máy chủ",
                    new List<ErrorDetail> {
                new ErrorDetail {
                    Code = "WebException",
                    Description = ex.Message
                }
                    });
            }
            catch (HttpRequestException)
            {
                return ApiResponse<TResponse>.Fail("Lỗi mạng", new List<ErrorDetail> { new ErrorDetail { Code = "NetworkError", Description = "Không có kết nối internet" } });
            }
            catch (TaskCanceledException)
            {
                return ApiResponse<TResponse>.Fail("Hết thời gian kết nối", new List<ErrorDetail> { new ErrorDetail { Code = "Timeout", Description = "Không có kết nối internet hoặc quá thời gian chờ" } });
            }



            // Đọc toàn bộ content ra string trước
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                ApiResponse<TResponse>? result = null;
                try
                {
                    result = JsonSerializer.Deserialize<ApiResponse<TResponse>>(content, jsonOptions);
                }
                catch (JsonException)
                {
                    // bỏ qua, fallback bên dưới sẽ xử lý
                }
                // ✅ Nếu server trả ApiResponse hợp lệ, luôn ưu tiên dùng nó (kể cả khi là lỗi)
                if (result != null)
                    return result;

                // fallback theo status code nếu không phải định dạng JSON chuẩn
                return response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized =>
                        ParseApiError<TResponse>(content, jsonOptions, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."),
                    HttpStatusCode.Forbidden =>
                        ParseApiError<TResponse>(content, jsonOptions, "Không có quyền truy cập"),
                    HttpStatusCode.NotFound =>
                        ParseApiError<TResponse>(content, jsonOptions, "Không tìm thấy tài nguyên. Có thể máy chủ đang khởi động, vui lòng thử lại"),
                    var code when (int)code >= 500 =>
                        ParseApiError<TResponse>(content, jsonOptions, "Máy chủ đang gặp sự cố. Vui lòng thử lại sau"),
                    var code when (int)code >= 400 =>
                        ParseApiError<TResponse>(content, jsonOptions, "Yêu cầu không hợp lệ hoặc không được phép"),
                    _ =>
                        ParseApiError<TResponse>(content, jsonOptions, "Có lỗi không xác định từ máy chủ")
                };
            }
            catch (JsonException jsonEx)
            {
                return ApiResponse<TResponse>.Fail("Không đọc được dữ liệu từ máy chủ",
                    new List<ErrorDetail> { new() { Code = "DeserializeError", Description = jsonEx.Message } });
            }
            catch (Exception ex)
            {
                return ApiResponse<TResponse>.Fail("Lỗi hệ thống khi đọc dữ liệu",
                    new List<ErrorDetail> { new() { Code = "UnhandledException", Description = ex.Message } });
            }
        }

        public async Task<ApiResponse<TResponse>> SendPublicPostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            return await SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, data, requireAuth: false);
        }
        public async Task<ApiResponse<TResponse>> SendRequestAsync<TResponse>(HttpMethod method, string endpoint, bool requireAuth = true)
        {
            return await SendRequestAsync<object, TResponse>(method, endpoint, null, requireAuth);
        }

        public async Task<ApiResponse<string>> UploadImageAsync(byte[] imageBytes)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

            form.Add(fileContent, "file", "upload.jpg");

            // Gọi API — đường dẫn upload là ví dụ, bạn điều chỉnh cho đúng với backend
            return await PostFormAsync<string>("/api/uploads/image", form);
        }
    }
}