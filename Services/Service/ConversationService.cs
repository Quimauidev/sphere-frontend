using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.ViewModels;
using System.Threading.Tasks;
using static Sphere.Models.Request;

namespace Sphere.Services.Service
{
    internal class ConversationService(IApiService apiService) : IConversationService
    {
        private readonly IApiService _apiService = apiService;
        public async Task<ApiResponse<MessageStartResponse>> StartConversationAsync(Guid id)
        {
            return await _apiService.PostAsync<object, MessageStartResponse>($"/api/conversations/start/{id}", null!);
        }

        public async Task<ApiResponse<IEnumerable<ConversationModel>>> GetConversationsAsync(int page, int pageSize)
        {
            return await _apiService.GetAsync<IEnumerable<ConversationModel>>($"/api/conversations?page={page}&pageSize={pageSize}");
        }

        public async Task<ApiResponse<IEnumerable<MessageModel>>> GetLatestMessagesAsync(Guid conversationId,int skip, int take)    
        {
            return await _apiService.GetAsync<IEnumerable<MessageModel>>(
                $"/api/conversations/{conversationId}/messages/latest?skip={skip}&take={take}");
        }

        public async Task<ApiResponse<Request.SendMessageRequest>> SendMessageAsync(Guid conversationId, Request.SendMessageRequest request)
        {
            return await _apiService.PostAsync<Request.SendMessageRequest, Request.SendMessageRequest>(
                $"/api/conversations/{conversationId}/messages", request);
        }

        public async Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId)
        {
            return await _apiService.PostAsync<object,object>(
               $"/api/conversations/{conversationId}/read", null!);
        }

        public async Task<ApiResponse<bool>> DeleteConversationAsync(Guid targetUserId)
        {
            return await _apiService.DeleteAsync<bool>($"/api/conversations/{targetUserId}");
        }

        public async Task<ApiResponse<CheckConversationResponse>> CheckConversationAsync(Guid id)
        {
            return await _apiService.GetAsync<CheckConversationResponse>($"/api/conversations/check-unlock/{id}");
        }
    }
}
