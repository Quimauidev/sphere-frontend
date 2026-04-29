using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Helpers;
using Sphere.Common.Responses;
using Sphere.Interfaces;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels.DiaryViewModels
{
    public partial class DiaryCommentViewModel(IDiaryService diaryService, ApiResponseHelper res) : BaseViewModel, IModalParameterReceiver<Guid>
    {
        private readonly ApiResponseHelper _res = res;
        private const int PageSize = 20;
        private readonly IDiaryService _diaryService = diaryService;
        private Guid _diaryId;

        private bool _isLoaded;
        private int _page = 1;
      
        [ObservableProperty]
        private Guid? editingCommentId;

        [ObservableProperty]
        private Guid? editingReplyToUserId;

        [ObservableProperty]
        private bool hasMoreComments = true;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isLoadingMore;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private int likeCount;

        [ObservableProperty]
        private string? newCommentContent;

        [ObservableProperty]
        private DiaryCommentUIModel? replyRoot;

        [ObservableProperty]
        private DiaryCommentUIModel? replyToComment;

        public ObservableCollection<DiaryCommentUIModel> Comments { get; } = [];
        public ObservableCollection<DiaryCommentFlatItem> FlatComments { get; } = [];
        public bool IsEditing => EditingCommentId != null;
        public bool IsReplying => ReplyToComment != null;

        public Action? RequestFocusCommentEditor { get; set; }

        public Action<DiaryCommentFlatItem>? ScrollToFlatItem { get; set; }

        public Action<int>? ScrollToIndex { get; set; }

        public Action<object>? ScrollToItem { get; set; }

        public Action<Guid, Guid>? ScrollToReplyInReplies { get; set; }

        [RelayCommand]
        public void CancelReply()
        {
            EditingReplyToUserId = null;
            ReplyToComment = null;
            NewCommentContent = string.Empty;
            ReplyRoot = null;
        }

        // like commnet và reply
        [RelayCommand]
        public async Task CommentLikeAsync(DiaryCommentFlatItem item)
        {
            if (item.IsBusy)
                return;

            item.IsBusy = true;

            // 🔥 UI đổi NGAY
            item.IsLiked = !item.IsLiked;
            item.LikeCount += item.IsLiked ? 1 : -1;

            try
            {
                var res = await _diaryService.SetCommentLikeAsync(item.Comment.Id);

                if (!res.IsSuccess)
                    await _res.ShowApiErrorsAsync(res, "Thao tác thích thất bại");

                // ✅ Sync nhẹ (chỉ khi lệch)
                if (item.IsLiked != res.Data!.IsLiked)
                    item.IsLiked = res.Data.IsLiked;

                item.LikeCount = res.Data.LikeCount;
            }
            catch
            {
                // ❌ rollback nếu fail
                item.IsLiked = !item.IsLiked;
                item.LikeCount += item.IsLiked ? 1 : -1;
            }
            finally
            {
                item.IsBusy = false;
            }
        }


        [RelayCommand]
        public async Task LoadCommentsAsync()
        {
            if (_isLoaded) return;
            _isLoaded = true;
            UiState = UiViewState.Loading;
            var res = await _diaryService.GetCommentAsync(_diaryId, 1, 20);
            if (!res.IsSuccess)
            {
                ErrorMessage = res.Errors?.FirstOrDefault()?.Description ?? res.Message ?? "Có lỗi xảy ra";
                UiState = UiViewState.Error;
                return;
            }

            Comments.Clear();
            foreach (var c in res.Data!)
                Comments.Add(c);
            BuildFlatComments();
            UiState = FlatComments.Count == 0 ? UiViewState.Empty : UiViewState.Success;
        }

        [RelayCommand]
        public async Task LoadMoreCommentsAsync()
        {
            if (IsLoadingMore || !HasMoreComments)
                return;

            IsLoadingMore = true;
            try
            {
                var nextPage = _page + 1;

                var res = await _diaryService.GetCommentAsync(_diaryId, nextPage, PageSize);
                if (!res.IsSuccess || res.Data == null || !res.Data.Any())
                {
                    HasMoreComments = false;
                    return;
                }
                _page = nextPage;
                var newComments = res.Data.ToList();

                foreach (var c in newComments)
                    Comments.Add(c);

                BuildFlatComments();

                // Nếu ít hơn pageSize → hết data
                if (newComments.Count < PageSize)
                    HasMoreComments = false;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        public async Task LoadMoreRepliesAsync(LoadMoreRepliesItem item)
        {
            var parent = Comments.FirstOrDefault(c => c.Id == item.ParentId);
            if (parent == null || parent.IsLoadingReplies)
                return;

            await LoadRepliesInternalAsync(parent, isFirstPage: false);
        }

        public async Task Receive(Guid parameter)
        {
            _diaryId = parameter;
            IsBusy = true;
            await LoadCommentsAsync();
            IsBusy = false;
        }

        [RelayCommand]
        public void Reply(DiaryCommentFlatItem item)
        {
            ReplyToComment = item.Comment;
            ReplyRoot = Comments.FirstOrDefault(c => c.Id == item.RootCommentId);

            NewCommentContent = string.Empty;
            RequestFocusCommentEditor?.Invoke();
            ScrollToFlatItem?.Invoke(item);
        }

        [RelayCommand]
        public async Task RetryAsync()
        {
            _isLoaded = false;
            ErrorMessage = null;
            await LoadCommentsAsync();
        }

        [RelayCommand]
        public async Task SendCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCommentContent))
                return;

            if (IsBusy) return;
            IsBusy = true;
            // ⭐ Ẩn bàn phím NGAY
            KeyboardService.HideKeyboard();
            try
            {
                var content = NewCommentContent!.Trim();
                // ================== ✏️ EDIT COMMENT ==================
                if (EditingCommentId != null)
                {
                    var update = await _diaryService.UpdateCommentAsync(EditingCommentId.Value, content, EditingReplyToUserId);
                    if (!update.IsSuccess)
                    {
                        await _res.ShowApiErrorsAsync(update, "Cập nhật bình luận thất bại");
                        return;
                    }

                    var item = FlatComments.First(x => x.Id == EditingCommentId.Value);
                    item.Comment.Content = content;

                    // ================= CASE: BỎ REPLY → THÀNH COMMENT GỐC =================
                    if (EditingReplyToUserId == null && item.Level == 1)
                    {
                        var oldRootId = item.RootCommentId;

                        // 1️⃣ Xóa khỏi replies của parent cũ
                        var oldParent = Comments.FirstOrDefault(c => c.Id == oldRootId);
                        oldParent?.Replies.Remove(item.Comment);

                        // 2️⃣ Reset quan hệ reply
                        item.Comment.ReplyToUserProfileId = null;
                        item.Comment.ReplyToFullName = null;
                        item.Comment.ParentCommentId = null;
                        // 🔥 CỰC QUAN TRỌNG
                        item.Comment.IsRepliesExpanded = false;
                        item.Comment.HasMoreReplies = false;
                        item.Comment.ReplyPage = 1;
                        item.Comment.Replies.Clear();

                        // 3️⃣ Đưa thành comment gốc (nếu chưa có trong Comments)
                        if (!Comments.Any(c => c.Id == item.Comment.Id))
                            Comments.Insert(0, item.Comment);

                        // 4️⃣ Rebuild flat list (CỰC KỲ QUAN TRỌNG)
                        BuildFlatComments();
                    }
                    else
                    {
                        // ================= CASE: VẪN LÀ REPLY =================
                        item.Comment.ReplyToUserProfileId = EditingReplyToUserId;
                        item.Comment.ReplyToFullName = ReplyToComment?.FullName;
                    }

                    ResetEditState();
                    return;
                }

                var replyId = ReplyRoot?.Id; // null nếu comment gốc
                var res = await _diaryService.CreateCommentAsync(_diaryId, NewCommentContent!.Trim(), replyId);
                if (!res.IsSuccess)
                {
                    await _res.ShowApiErrorsAsync(res, "Gửi bình luận thất bại");
                }
                if (replyId == null)
                {
                    Comments.Insert(0, res.Data!);
                    BuildFlatComments(); // Cập nhật lại FlatComments để hiển thị comment gốc mới
                }                
                else
                {
                    var root = ReplyRoot;
                    if (root == null)
                        return; // không crash, không gửi
                   

                    res.Data!.ParentCommentId = root.Id;
                    res.Data.IsLocal = true;
                    root.Replies.Add(res.Data!);
                    InsertNewReplies(root, new List<DiaryCommentUIModel> { res.Data! });

                    // scroll tới reply mới
                    // 🔥 SCROLL ĐÚNG UX
                    if (root.HasMoreReplies)
                    {
                        // còn replies chưa load → scroll tới "Xem thêm"
                        ScrollOnFirstExpand(root);
                    }
                    else
                    {
                        // đã load hết → scroll tới reply mới
                        ScrollToLastReply(root);
                    }
                }
                if (UiState == UiViewState.Empty)
                    UiState = UiViewState.Success;

                NewCommentContent = null;
                ReplyToComment = null;
                ReplyRoot = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ToggleRepliesAsync(DiaryCommentFlatItem item)
        {
            var parent = item.Comment;

            // 🔽 ĐÓNG
            if (parent.IsRepliesExpanded)
            {
                parent.IsRepliesExpanded = false;
                parent.ReplyPage = 1;
                parent.Replies.Clear();
                RemoveReplies(parent.Id);
                return;
            }

            // 🔼 MỞ → load page đầu, KHÔNG scroll, KHÔNG rebuild toàn bộ replies
            await LoadRepliesInternalAsync(parent, isFirstPage: true);
        }

        private void BuildFlatComments()
        {
            var cache = FlatComments.ToDictionary(x => x.Id);

            FlatComments.Clear();

            foreach (var parent in Comments)
            {
                FlatComments.Add(CreateOrReuse(parent, 0, parent.Id, cache));

                if (parent.Replies != null)
                {
                    foreach (var reply in parent.Replies)
                        FlatComments.Add(CreateOrReuse(reply, 1, parent.Id, cache));
                }

                // ⭐⭐⭐ THÊM ĐOẠN NÀY
                if (parent.HasMoreReplies)
                {
                    FlatComments.Add(CreateLoadMoreItem(parent.Id));
                }
            }
        }

        // helper factory to create a new flat item
        private DiaryCommentFlatItem CreateFlatItem(DiaryCommentUIModel comment, int level, Guid rootId)
            => new DiaryCommentFlatItem
            {
                Id = comment.Id,
                Comment = comment,
                Level = level,
                RootCommentId = rootId,
                IsLiked = comment.IsLiked,
                LikeCount = comment.LikeCount,
                RequestEdit = OnEditCommentRequested,
                RequestDelete = OnDeleteCommentRequested
            };

        private LoadMoreRepliesItem CreateLoadMoreItem(Guid parentId)
            => new LoadMoreRepliesItem
            {
                ParentId = parentId,
                Level = 1,
                RootCommentId = parentId
            };

        // Update CreateOrReuse so cached flat items are refreshed with current level/root/comment when reused
        private DiaryCommentFlatItem CreateOrReuse(DiaryCommentUIModel comment, int level, Guid rootId, Dictionary<Guid, DiaryCommentFlatItem> cache)
        {
            if (cache.TryGetValue(comment.Id, out var item))
            {
                // refresh fields to reflect current hierarchy and state
                item.Level = level;
                item.RootCommentId = rootId;
                item.Comment = comment;
                item.IsLiked = comment.IsLiked;
                item.LikeCount = comment.LikeCount;
                // ensure actions are set (in case cached item was different)
                item.RequestEdit = OnEditCommentRequested;
                item.RequestDelete = OnDeleteCommentRequested;
                return item;
            }

            return CreateFlatItem(comment, level, rootId);
        }

        private void InsertNewReplies(DiaryCommentUIModel parent, List<DiaryCommentUIModel> newReplies)
        {
            // ❗ Xóa LoadMore cũ
            var loadMore = FlatComments
                .OfType<LoadMoreRepliesItem>()
                .FirstOrDefault(x => x.ParentId == parent.Id);

            if (loadMore != null)
                FlatComments.Remove(loadMore);

            var parentIndex = FlatComments
                .Select((x, i) => new { x, i })
                .First(x => x.x.Id == parent.Id)
                .i;

            // Insert sau replies hiện tại
            var insertIndex = parentIndex + 1;

            // nhảy qua replies hiện có
            while (insertIndex < FlatComments.Count &&
                   FlatComments[insertIndex].Level == 1 &&
                   FlatComments[insertIndex].RootCommentId == parent.Id)
            {
                insertIndex++;
            }

            foreach (var reply in newReplies)
            {
                FlatComments.Insert(insertIndex++,
                    CreateFlatItem(reply, 1, parent.Id));
            }

            // ⭐ Thêm LoadMore nếu còn
            if (parent.HasMoreReplies)
            {
                FlatComments.Insert(insertIndex,
                    CreateLoadMoreItem(parent.Id));
            }
        }

        private void InsertRepliesBeforeLoadMore(DiaryCommentUIModel parent, List<DiaryCommentUIModel> newReplies)
        {
            if (newReplies == null || newReplies.Count == 0)
                return;
            // Tìm vị trí dòng LoadMore
            var loadMoreIndex = FlatComments.Select((x, i) => new { x, i })
                .FirstOrDefault(x => x.x is LoadMoreRepliesItem lm && lm.ParentId == parent.Id)?.i;
            if (loadMoreIndex == null)
                return;
            var insertIndex = loadMoreIndex.Value;
            foreach (var reply in newReplies)
            {
                FlatComments.Insert(insertIndex++, CreateFlatItem(reply, 1, parent.Id));
            }
            // Nếu hết replies thì xóa dòng LoadMore
            if (!parent.HasMoreReplies)
            {
                var loadMore = FlatComments.OfType<LoadMoreRepliesItem>().FirstOrDefault(x => x.ParentId == parent.Id);
                if (loadMore != null)
                    FlatComments.Remove(loadMore);
            }
        }

        private void InsertRepliesOnExpand(DiaryCommentUIModel parent, List<DiaryCommentUIModel> replies)
        {
            // Xóa replies cũ (nếu có) và dòng LoadMore cũ
            RemoveReplies(parent.Id);
            var parentIndex = FlatComments.Select((x, i) => new { x, i }).First(x => x.x.Id == parent.Id).i;
            var insertIndex = parentIndex + 1;
            foreach (var reply in replies)
            {
                FlatComments.Insert(insertIndex++, CreateFlatItem(reply, 1, parent.Id));
            }
            if (parent.HasMoreReplies)
            {
                FlatComments.Insert(insertIndex, CreateLoadMoreItem(parent.Id));
            }
        }

        private async Task LoadRepliesInternalAsync(DiaryCommentUIModel parent, bool isFirstPage)
        {
            parent.IsLoadingReplies = true;

            var res = await _diaryService.GetRepliesAsync(
                parent.Id,
                parent.ReplyPage,
                parent.ReplyPageSize);

            parent.IsLoadingReplies = false;

            if (!res.IsSuccess || res.Data == null)
                return;

            var newReplies = res.Data.ToList();

            if (isFirstPage)
            {
                // Giữ lại reply client-side (chưa có trên server page 1)
                var clientReplies = parent.Replies.Where(r => r.IsLocal).ToList();
                parent.Replies.Clear();
                foreach (var r in clientReplies)
                    parent.Replies.Add(r);
                foreach (var r in newReplies)
                {
                    if (!parent.Replies.Any(x => x.Id == r.Id))
                        parent.Replies.Add(r);
                }
                foreach (var r in parent.Replies)
                {
                    if (newReplies.Any(x => x.Id == r.Id))
                        r.IsLocal = false;
                }
                parent.HasMoreReplies = newReplies.Count == parent.ReplyPageSize;
                parent.ReplyPage++;
                parent.IsRepliesExpanded = true;
                // Chỉ insert replies vào FlatComments, không scroll
                InsertRepliesOnExpand(parent, parent.Replies.ToList());
            }
            else
            {
                // Khi load thêm, chỉ insert replies mới vào FlatComments trước dòng LoadMore
                var repliesToInsert = newReplies.Where(r => !parent.Replies.Any(x => x.Id == r.Id)).ToList();
                foreach (var r in repliesToInsert)
                    parent.Replies.Add(r);
                foreach (var r in parent.Replies)
                {
                    if (newReplies.Any(x => x.Id == r.Id))
                        r.IsLocal = false;
                }
                parent.HasMoreReplies = newReplies.Count == parent.ReplyPageSize;
                parent.ReplyPage++;
                parent.IsRepliesExpanded = true;
                InsertRepliesBeforeLoadMore(parent, repliesToInsert);
            }
        }

        private async void OnDeleteCommentRequested(DiaryCommentFlatItem item)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            Console.WriteLine($"[DeleteComment] Start - CommentId: {item.Id}");
            var res = await _diaryService.DeleteCommentAsync(item.Id);

            if (!res.IsSuccess)
            {
                Console.WriteLine($"[DeleteComment] FAILED - CommentId: {item.Id}");
                Console.WriteLine($"Message: {res.Message}");
                Console.WriteLine($"Errors: {string.Join(", ", res.Errors?.Select(e => e.Description) ?? [])}");
                await _res.ShowApiErrorsAsync(res, "Xóa bình luận thất bại");
                IsBusy = false;
                return;
            }

            // ================== REMOVE UI ==================

            // 1️⃣ Nếu đang edit comment này → reset
            if (EditingCommentId == item.Id)
            {
                EditingCommentId = null;
                NewCommentContent = null;
            }

            // 2️⃣ Nếu đang reply vào comment này → reset
            if (ReplyToComment?.Id == item.Id)
            {
                ReplyToComment = null;
                ReplyRoot = null;
            }

            // 3️⃣ REMOVE PARENT COMMENT
            if (item.IsParent)
            {
                var parent = Comments.FirstOrDefault(c => c.Id == item.Id);
                if (parent != null)
                    Comments.Remove(parent);

                // Xóa toàn bộ flat items liên quan
                RemoveReplies(item.Id);
                FlatComments.Remove(item);
            }
            // 4️⃣ REMOVE REPLY
            else
            {
                var parent = Comments.FirstOrDefault(c => c.Id == item.RootCommentId);
                parent?.Replies.Remove(item.Comment);

                FlatComments.Remove(item);
            }

            // 5️⃣ Cập nhật UiState
            if (Comments.Count == 0)
                UiState = UiViewState.Empty;

            IsBusy = false;
        }

        private void OnEditCommentRequested(DiaryCommentFlatItem item)
        {
            EditingCommentId = item.Id;
            NewCommentContent = item.Comment.Content;
            if (item.Comment.HasReplyTo && item.Comment.ReplyToUserProfileId.HasValue)
            {
                EditingReplyToUserId = item.Comment.ReplyToUserProfileId;
                ReplyToComment = new DiaryCommentUIModel
                {
                    Id = item.Comment.ReplyToUserProfileId.Value,
                    FullName = item.Comment.ReplyToFullName
                };

                ReplyRoot = Comments.FirstOrDefault(c => c.Id == item.RootCommentId);
            }
            else
            {
                // Khi edit thì KHÔNG reply
                ReplyToComment = null;
                ReplyRoot = null;
            }
            // Focus Editor
            RequestFocusCommentEditor?.Invoke();

            // Scroll tới comment đang edit (optional nhưng UX rất tốt)
            ScrollToFlatItem?.Invoke(item);
        }

        partial void OnReplyToCommentChanged(DiaryCommentUIModel? value)
        {
            OnPropertyChanged(nameof(IsReplying));
        }

        [RelayCommand]
        private async Task RefreshCommentsAsync()
        {
            IsRefreshing = true;
            _isLoaded = false;
            _page = 1;
            Comments.Clear();
            FlatComments.Clear();
            await LoadCommentsAsync();
            IsRefreshing = false;
        }
        private void RemoveReplies(Guid parentId)
        {
            var items = FlatComments.Where(x => (x.Level == 1 && x.RootCommentId == parentId) || x is LoadMoreRepliesItem lm && lm.ParentId == parentId).ToList();

            foreach (var i in items)
                FlatComments.Remove(i);
        }

        private void ResetEditState()
        {
            EditingCommentId = null;
            EditingReplyToUserId = null;
            NewCommentContent = null;
            ReplyToComment = null;
            ReplyRoot = null;
        }
        private void ScrollOnFirstExpand(DiaryCommentUIModel parent)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(30);

                var loadMore = FlatComments
                    .OfType<LoadMoreRepliesItem>()
                    .FirstOrDefault(x => x.ParentId == parent.Id);

                if (loadMore != null)
                {
                    ScrollToItem?.Invoke(loadMore);
                    return;
                }

                ScrollToLastReply(parent);
            });
        }

        private void ScrollToLastReply(DiaryCommentUIModel parent)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(30);

                var lastReply = FlatComments
                    .Where(x => x.Level == 1 && x.RootCommentId == parent.Id)
                    .LastOrDefault();

                if (lastReply != null)
                    ScrollToItem?.Invoke(lastReply);
            });
        }
    }
}