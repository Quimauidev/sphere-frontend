using Sphere.Common.Responses;

namespace Sphere.Services.IService
{
    internal interface IApiService
    {
        Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint);

        Task<ApiResponse<TResponse>> GetAsync<TResponse>(string endpoint);

        Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data);

        Task<ApiResponse<TResponse>> PatchFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData);

        Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);

        Task<ApiResponse<TResponse>> PostFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData);

        Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<ApiResponse<TResponse>> PutFormAsync<TResponse>(string endpoint, MultipartFormDataContent formData);
        Task<ApiResponse<TResponse>> SendRequestAsync<TRequest, TResponse>( HttpMethod method, string endpoint, TRequest data, bool requireAuth = true);
        Task<ApiResponse<TResponse>> SendRequestAsync<TResponse>( HttpMethod method, string endpoint, bool requireAuth = true);

        Task<ApiResponse<TResponse>> SendPublicPostAsync<TRequest, TResponse>( string endpoint, TRequest data);

        Task<ApiResponse<string>> UploadImageAsync(byte[] imageBytes);

    }
}