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
        Task<ApiResponse<Request.MessageStartResponse>> StartConversationAsync(Request.ChatStartRequest request);
        Task<ApiResponse<IEnumerable<MessageModel>>> GetMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50);
        Task<ApiResponse<Request.SendMessageRequest>> SendMessageAsync(Guid conversationId, Request.SendMessageRequest request);
        Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId);
        Task<ApiResponse<bool>> DeleteConversationAsync(Guid targetUserId);
    }
}
