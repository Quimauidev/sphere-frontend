using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Common.Constans;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public partial class MessageModel : ObservableObject
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

        [ObservableProperty]
        private bool isLastMessage;

        // Thuộc tính phụ cho UI (frontend-only)
        [ObservableProperty]
        private bool isMine;

        [ObservableProperty]
        private bool showStatusIcon;

        [ObservableProperty]
        private MessageStatus status;
        public bool IsSending => Status == MessageStatus.Sending;


        partial void OnStatusChanged(MessageStatus value)
        {
            // Raise change for computed property so UI updates icon when Status changes
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(IsSending));
        }

        // Hiển thị icon hoặc màu cho trạng thái
        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    MessageStatus.Sent => "\U000F012C",       // sent
                    MessageStatus.Delivered => "\U000F1CA3", // delivered
                    MessageStatus.Seen => "\U000F0208",     // seen
                    _ => ""
                };
            }
        }
    }

    public class MessageEntity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }

        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsRead { get; set; }
        public bool IsRecalled { get; set; }

        public DateTime SentAt { get; set; }

        public int Status { get; set; }   // int để lưu enum
    }
}
