using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public static class Request
    {
        public class ChatStartRequest
        {
            public Guid TargetUserId { get; set; }
        }
        public class MessageStartResponse
        {
            public bool IsUnlocked { get; set; }
            public bool IsFirstUnlock { get; set; }
            public long NewBalance { get; set; }
            public Guid? ConversationId { get; set; }
        }
        public class CheckConversationResponse
        {
            public bool IsUnlocked { get; set; }
            public bool CanUnlock { get; set; }
            public long RequiredDiamonds { get; set; }
            public Guid? ConversationId { get; set; }
        }

        public class SendMessageRequest
        {
            public Guid Id { get; set; }
            public string? Content { get; set; }
            public string? MediaUrl { get; set; }
            public string? MediaType { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }
    }
}
