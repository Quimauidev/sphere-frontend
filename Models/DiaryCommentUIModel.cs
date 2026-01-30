using CommunityToolkit.Mvvm.ComponentModel;
using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{

    public partial class DiaryCommentUIModel : ObservableObject
    {
        public Guid Id { get; set; } // ùng để phân biệt comment
        public Guid? ParentCommentId { get; set; } // ùng để phân biệt comment cha
        public Guid UserProfileId { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public Gender Gender { get; set; }
        // ⭐ Quan trọng
        public Guid? ReplyToUserProfileId { get; set; }
        public string? ReplyToFullName { get; set; }
        [ObservableProperty]
        public string? content;
        public DateTime CommentedAt { get; set; }
        public int LikeCount { get; set; }
        public bool IsLiked { get; set; }
        public int ReplyCount { get; set; }
        public bool IsOwner { get; set; }
        //// UI
        public ObservableCollection<DiaryCommentUIModel> Replies { get; set; } = [];
        public bool HasReplies => ReplyCount > 0;
        public bool HasReplyTo => !string.IsNullOrWhiteSpace(ReplyToFullName);

        [ObservableProperty]
        private bool isRepliesExpanded;
        [ObservableProperty]
        private bool isLoadingReplies;
        
        public string AvatarDisplay => !string.IsNullOrWhiteSpace(AvatarUrl) ? AvatarUrl : Gender == Gender.Male ? "man.png" : "woman.png";
        public int ReplyPage { get; set; } = 1;
        public int ReplyPageSize { get; set; } = 10;
        [ObservableProperty]
        private bool hasMoreReplies;
        public bool IsLocal { get; set; }

    }

    public class PostDiaryCommentModel
    {
        public Guid? ParentCommentId { get; set; }
        public string? Content { get; set; }
    }


}
