using Sphere.Common.Responses;

namespace Sphere.Services.IService
{
    internal interface IApiService
    {
        Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> PatchFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> PostFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default);
        Task<ApiResponse<TResponse>> PutFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData, CancellationToken ct = default);
        Task<ApiResponse<TResponse>> SendRequestAsync<TRequest, TResponse>( HttpMethod method, string endpoint, TRequest data, bool requireAuth, CancellationToken ct);
        Task<ApiResponse<TResponse>> SendRequestAsync<TResponse>( HttpMethod method, string endpoint, CancellationToken ct = default);

        Task<ApiResponse<TResponse>> SendPublicPostAsync<TRequest, TResponse>( string endpoint, TRequest data, CancellationToken ct = default);

        Task<ApiResponse<string>> UploadImageAsync(byte[] imageBytes);

    }
}