using CommunityToolkit.Mvvm.ComponentModel;
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
        public bool HasReplies => ReplyCount > 0;
        [ObservableProperty]
        private bool isRepliesExpanded;
        [ObservableProperty]
        private bool isLoadingReplies;

    }

    

    public class PostDiaryCommentModel
    {
        public Guid? ParentCommentId { get; set; }
        public string? Content { get; set; }
    }


}
