using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Responses;

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
      
        public Action<DiaryCommentFlatItem>? RequestEdit { get; set; }
        public Action<DiaryCommentFlatItem>? RequestDelete { get; set; }


        // ✅ THÊM ĐOẠN NÀY
        [RelayCommand]
        private async Task ShowOptionsAsync()
        {
            if (Comment == null)
                return;

            string? action;

            if (Comment.IsOwner)
            {
                // 👑 Comment của mình
                action = await Shell.Current.DisplayActionSheetAsync( "Tùy chọn", "Hủy", null, "Chỉnh sửa", "Xóa", "Sao chép");
            }
            else
            {
                // 👤 Comment người khác
                action = await Shell.Current.DisplayActionSheetAsync( "Tùy chọn", "Hủy", null, "Tố cáo", "Sao chép");
            }

            if (string.IsNullOrEmpty(action) || action == "Hủy")
                return;

            switch (action)
            {
                case "Chỉnh sửa":
                    HandleEdit();
                    break;

                case "Xóa":
                    await HandleDeleteAsync();
                    break;

                case "Tố cáo":
                    await HandleReportAsync();
                    break;

                case "Sao chép":
                    await Clipboard.Default.SetTextAsync(Comment.Content ?? string.Empty);
                    break;
            }
        }
        private void HandleEdit()
        {
            RequestEdit?.Invoke(this);
        }

        private async Task HandleDeleteAsync()
        {
            bool confirm = await ApiResponseHelper.ShowShellConfirmAsync("Xóa bình luận", "Bạn chắc chắn muốn xóa?", "Xóa", "Hủy");

            if (!confirm)
                return;

            RequestDelete?.Invoke(this);
        }


        private static async Task HandleReportAsync()
        {
            await ApiResponseHelper.ShowShellAlertAsync("Tố cáo", "Bình luận đã được gửi tố cáo");

            // TODO: gọi API report
        }

    }
    public class LoadMoreRepliesItem : DiaryCommentFlatItem
    {
        public Guid ParentId { get; set; }
    }

}
