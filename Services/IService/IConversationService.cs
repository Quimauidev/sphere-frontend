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
        Task<ApiResponse<IEnumerable<ConversationModel>>> GetConversationsAsync(int page, int pageSize);
        Task<ApiResponse<MessageStartResponse>> StartConversationAsync(Guid id);    
        Task<ApiResponse<CheckConversationResponse>> CheckConversationAsync(Guid targetUserId);
        Task<ApiResponse<IEnumerable<MessageModel>>> GetLatestMessagesAsync(Guid conversationId,int skip, int take);
        Task<ApiResponse<SendMessageRequest>> SendMessageAsync(Guid conversationId, SendMessageRequest request);
        Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId);
        Task<ApiResponse<bool>> DeleteConversationAsync(Guid targetUserId);
    }
}
