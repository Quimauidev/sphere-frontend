namespace Sphere.Models
{
    public class DiaryCommentFlatItem
    {
        public Guid Id { get; set; }
        public DiaryCommentUIModel Comment { get; set; } = null!;
        public int Level { get; set; } // 0 = parent, 1 = reply
        public bool IsParent => Level == 0;
        public bool IsReply => Level == 1;
        public Guid RootCommentId { get; set; }
    }
}
