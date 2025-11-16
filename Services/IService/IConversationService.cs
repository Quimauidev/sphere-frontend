using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sphere.Models.Request;

namespace Sphere.Services.IService
{
    public interface IConversationService
    {
        Task<ApiResponse<IEnumerable<ConversationModel>>> GetConversationsAsync();
        Task<ApiResponse<MessageStartResponse>> StartConversationAsync(Guid id);
        Task<ApiResponse<IEnumerable<MessageModel>>> GetLatestMessagesAsync(Guid conversationId, int take = 50);
        Task<ApiResponse<IEnumerable<MessageModel>>> GetMessagesBeforeAsync(Guid conversationId, Guid messageId, int take = 50);
        Task<ApiResponse<SendMessageRequest>> SendMessageAsync(Guid conversationId, SendMessageRequest request);
        Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId);
        Task<ApiResponse<bool>> DeleteConversationAsync(Guid targetUserId);
    }
}
