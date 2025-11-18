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
        public Guid PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public string? PartnerAvatar { get; set; }
        public string? LastMessage { get; set; }

        public DateTime LastUpdatedAt { get; set; } // thời gian tin nhắn cuối cùng
        public bool IsDeletedForCurrentUser { get; set; }
        public bool IsOnline { get; set; } // thêm cho UI realtime
                                           // Thuật toán hiển thị thời gian cho UI, ví dụ "HH:mm" hoặc "1 giờ trước"
        public string LastUpdatedText => LastUpdatedAt.ToString("HH:mm");
    }


}
