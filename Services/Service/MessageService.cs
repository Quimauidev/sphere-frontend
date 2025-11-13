using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    internal class MessageService : IMessageService
    {
        private readonly IApiService _apiService;
        public MessageService(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ApiResponse<Request.MessageStartResponse>> StartConversationAsync(Guid id)
        {
            return await _apiService.PostAsync<object,Request.MessageStartResponse>($"api/conversations/start/{id}", null!);
        }
    }
}
