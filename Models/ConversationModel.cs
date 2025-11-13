using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class ConversationModel
    {
        public Guid Id { get; set; }
        public string? PartnerName { get; set; }
        public string? PartnerAvatar { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public string? LastUpdatedText { get; set; } // ví dụ: "5 phút trước"
        public bool IsOnline { get; set; } // thêm cho UI realtime
    }
}
