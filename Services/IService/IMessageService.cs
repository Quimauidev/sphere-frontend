using Sphere.Common.Responses;
using Sphere.Models;
using System.Threading.Tasks;
using static Sphere.Models.Request;

namespace Sphere.Services.IService
{
    public interface IMessageService
    {
        Task<ApiResponse<MessageStartResponse>> StartConversationAsync(Guid id);
    }
}
