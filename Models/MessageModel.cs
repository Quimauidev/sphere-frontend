using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class MessageModel
    {
        public Guid Id { get; set; } // ID tin nhắn
        public Guid ConversationId { get; set; } // ID cuộc trò chuyện
        public Guid SenderId { get; set; } // ID người gửi
        public Guid ReceiverId { get; set; } // ID người nhận

        public string? Content { get; set; } // nội dung tin nhắn
        public string? MediaUrl { get; set; } // URL media (nếu có)
        public string? MediaType { get; set; } // loại media (image, video, audio, file)
        public double? Latitude { get; set; } // vĩ độ (nếu có)
        public double? Longitude { get; set; } // kinh độ (nếu có)

        public bool IsRead { get; set; } // trạng thái đã đọc
        public bool IsRecalled { get; set; } // trạng thái đã thu hồi
        public DateTime SentAt { get; set; } // thời gian gửi

        // Thuộc tính phụ cho UI (frontend-only)
        public bool IsMine { get; set; } // để biết là tin mình gửi hay tin nhận
        public string? DisplayTime => SentAt.ToLocalTime().ToString("HH:mm"); // định dạng thời gian hiển thị
        public bool HasMedia => !string.IsNullOrEmpty(MediaUrl); // kiểm tra có media không
        public bool HasLocation => Latitude.HasValue && Longitude.HasValue; // kiểm tra có vị trí không
    }
}
