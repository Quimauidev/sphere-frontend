using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    //public class DiaryCommentResponseDTO
    //{
    //    public Guid Id { get; set; }
    //    public Guid UserProfileId { get; set; }
    //    public string? FullName { get; set; }
    //    public string? AvatarUrl { get; set; }

    //    public Guid? ReplyToUserProfileId { get; set; }
    //    public string? ReplyToFullName { get; set; }

    //    public string? Content { get; set; }
    //    public DateTime CommentedAt { get; set; }
    //    public int LikeCount { get; set; }
    //    public bool IsLiked { get; set; }

    //    public int ReplyCount { get; set; }
    //    public bool IsOwner { get; set; }
    //}

    public class DiaryCommentUIModel
    {
        public Guid Id { get; set; }
        public Guid? ParentCommentId { get; set; } 
        public Guid UserProfileId { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        // ⭐ Quan trọng
        public Guid? ReplyToUserProfileId { get; set; }
        public string? ReplyToFullName { get; set; }
        public string? Content { get; set; }
        public DateTime CommentedAt { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }

        public int ReplyCount { get; set; }
        public bool IsOwner { get; set; }
        //// UI
        public ObservableCollection<DiaryCommentUIModel> Replies { get; set; } = [];
        public bool IsRepliesLoaded { get; set; } //Dùng cho:  Lazy-load replies(nếu cần)  Tránh load lại nhiều lần
    }

    //public static class DiaryCommentMapper
    //{
    //    public static DiaryCommentUIModel MapToUI(this DiaryCommentResponseDTO dto)
    //        => new()
    //        {
    //            Id = dto.Id,
    //            UserProfileId = dto.UserProfileId,
    //            FullName = dto.FullName,
    //            AvatarUrl = dto.AvatarUrl,
    //            ReplyToUserProfileId = dto.ReplyToUserProfileId,
    //            ReplyToFullName = dto.ReplyToFullName,
    //            Content = dto.Content,
    //            CommentedAt = dto.CommentedAt,
    //            LikeCount = dto.LikeCount,
    //            IsLiked = dto.IsLiked,
    //            ReplyCount = dto.ReplyCount,
    //            IsOwner = dto.IsOwner
    //        };
    //}

    public class PostDiaryCommentModel
    {
        public Guid? ParentCommentId { get; set; }
        public string? Content { get; set; }
    }


}
