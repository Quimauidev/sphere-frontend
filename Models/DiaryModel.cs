using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class DiaryModel : ObservableObject
    {
        public Guid Id { get; set; } // ID nhật ký

        public string? Content { get; set; } // Nội dung

        public List<DiaryImageDTO>? Images { get; set; } // Ảnh đính kèm

        public DateTime CreatedAt { get; set; } // Ngày tạo

        public int ViewCount { get; set; } = 0; // Lượt xem

        public int LikeCount { get; set; } = 0; // Lượt thích
        public bool IsLiked { get; set; } // Đã thích hay chưa
        public int CommentCount { get; set; } = 0; // Lượt bình luận

        public Privacy Privacy { get; set; } 
        public bool IsOwNer { get; set; }
        public bool HasImages => (Images?.Count ?? 0) > 0;
        public double ImageItemHeight
        {
            get
            {
                int count = Images?.Count ?? 0;
                if (count == 0) return 0;
                if (count <= 3) return 250;
                if (count <= 6) return 170;
                return 120;
            }
        }

        public double ImageItemWidth
        {
            get
            {
                int count = Images?.Count ?? 0;
                if (count == 0) return 0;
                double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                // Trừ margin ngoài và padding trong Frame
                double actualWidth = screenWidth - 46;

                return count switch
                {
                    <= 1 => 250,
                    2 => actualWidth / 2,
                    _ => (actualWidth - 2) / 3
                };
            }
        }
    }

    public class DiaryImageDTO
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class UserWithDiaryModel
    {
        public DiaryModel? DiaryDTO { get; set; } 
        public UserDiaryModel? UserDiaryDTO { get; set; }
    }

    public class UserDiaryModel
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public bool IsFollow { get; set; }
        public Gender Gender { get; set; }
    }
    public class DiaryLikeStatusDTO
    {
        public bool IsLiked { get; set; }
        public int LikeCount { get; set; }
    }
}