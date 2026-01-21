using CommunityToolkit.Mvvm.ComponentModel;

namespace Sphere.Models
{
    public partial class DiaryCommentFlatItem : ObservableObject
    {
        public Guid Id { get; set; }
        public DiaryCommentUIModel Comment { get; set; } = null!;
        public int Level { get; set; } // 0 = parent, 1 = reply
        public bool IsParent => Level == 0;
        public bool IsReply => Level == 1;
        public Guid RootCommentId { get; set; }
        [ObservableProperty]
        private bool isLiked;

        [ObservableProperty]
        private int likeCount;

        [ObservableProperty]
        private bool isBusy;
    }
    public class LoadMoreRepliesItem : DiaryCommentFlatItem
    {
        public Guid ParentId { get; set; }
    }

}
