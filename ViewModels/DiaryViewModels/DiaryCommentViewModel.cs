using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntelliJ.Lang.Annotations;
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
using static Android.Graphics.ColorSpace;

namespace Sphere.ViewModels.DiaryViewModels
{
    public partial class DiaryCommentViewModel : ObservableObject, IModalParameterReceiver<Guid>
    {
        private readonly IDiaryService _diaryService;
        private Guid _diaryId;

        public ObservableCollection<DiaryCommentUIModel> Comments { get; } = [];    
        public ObservableCollection<DiaryCommentFlatItem> FlatComments { get; } = [];

        public DiaryCommentViewModel(IDiaryService diaryService)
        {
            _diaryService = diaryService;
            
        }
        private bool _isLoaded;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private DiaryCommentUIModel? replyToComment;
        public bool IsReplying => ReplyToComment != null;
        partial void OnReplyToCommentChanged(DiaryCommentUIModel? value)
        {
            OnPropertyChanged(nameof(IsReplying));
        }
        public Guid? ParentCommentId { get; set; }

        [ObservableProperty]
        private string? newCommentContent;

        public Action<int>? ScrollToIndex { get; set; }
        public Action<DiaryCommentFlatItem>? ScrollToFlatItem { get; set; }
        public Action<object>? ScrollToItem { get; set; }


        public Action? RequestFocusCommentEditor { get; set; }

        public Action<Guid, Guid>? ScrollToReplyInReplies { get; set; }

        [ObservableProperty]
        private int likeCount;

        [ObservableProperty]
        private string? errorMessage;

        [RelayCommand]
        public async Task LoadCommentsAsync()
        {
            if (_isLoaded) return;
            _isLoaded = true;
            var res = await _diaryService.GetCommentAsync(_diaryId, 1, 20);
            if (!res.IsSuccess)
                {
                ErrorMessage = res.Errors?.FirstOrDefault()?.Description ?? res.Message ?? "Có lỗi xảy ra";
                return;
            }

            Comments.Clear();
            foreach (var c in res.Data!)
                Comments.Add(c);
            BuildFlatComments();
        }
        //private void BuildFlatComments()
        //{
        //    FlatComments.Clear();

        //    foreach (var parent in Comments)
        //    {
        //        FlatComments.Add(new DiaryCommentFlatItem
        //        {
        //            Id = parent.Id,
        //            Comment = parent,
        //            Level = 0,
        //            RootCommentId = parent.Id,
        //            IsLiked = parent.IsLiked,
        //            LikeCount = parent.LikeCount
        //        });

        //        foreach (var reply in parent.Replies)
        //        {
        //            FlatComments.Add(new DiaryCommentFlatItem
        //            {
        //                Id = reply.Id,
        //                Comment = reply,
        //                Level = 1,
        //                RootCommentId = parent.Id,
        //                 IsLiked = reply.IsLiked,
        //                LikeCount = reply.LikeCount
        //            });
        //        }
        //    }
        //}
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
                    FlatComments.Add(new LoadMoreRepliesItem
                    {
                        ParentId = parent.Id,
                        Level = 1,
                        RootCommentId = parent.Id
                    });
                }
            }
        }

        private DiaryCommentFlatItem CreateOrReuse(
            DiaryCommentUIModel comment,
            int level,
            Guid rootId,
            Dictionary<Guid, DiaryCommentFlatItem> cache)
        {
            if (cache.TryGetValue(comment.Id, out var item))
                return item;

            return new DiaryCommentFlatItem
            {
                Id = comment.Id,
                Comment = comment,
                Level = level,
                RootCommentId = rootId,
                IsLiked = comment.IsLiked,
                LikeCount = comment.LikeCount
            };
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
                var replyId = ReplyToComment?.Id;
                var res = await _diaryService.CreateCommentAsync(_diaryId, NewCommentContent!.Trim(), replyId);
                if (res.IsSuccess)
                {
                    if (replyId == null)
                    {
                        Comments.Insert(0, res.Data!);
                    }
                    else
                    {
                        
                       
                        var root = Comments.FirstOrDefault(c => c.Id == ParentCommentId);

                        if (root == null)
                            throw new InvalidOperationException("Root comment not found");

                        // 2️⃣ ÉP ParentCommentId về root (QUAN TRỌNG)
                        res.Data!.ParentCommentId = root.Id;
                        res.Data!.IsLocal = true; // ⭐ QUAN TRỌNG
                        // 🔥 QUAN TRỌNG
                        // luôn thêm reply mới vào Replies (client-side)
                        root.Replies.Add(res.Data!);

                        // nhưng KHÔNG đổi ReplyPage, HasMoreReplies
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
                    NewCommentContent = null;
                    ReplyToComment = null;
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(res, "Gửi bình luận thất bại");
                }
                
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void Reply(DiaryCommentFlatItem item)
        {
            ReplyToComment = item.Comment;
            ParentCommentId = item.RootCommentId;
            NewCommentContent = string.Empty;
            RequestFocusCommentEditor?.Invoke();
            ScrollToFlatItem?.Invoke(item);
        }

        [RelayCommand]
        public void CancelReply()
        {
            ReplyToComment = null;
            NewCommentContent = string.Empty;
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
                    await ApiResponseHelper.ShowApiErrorsAsync(res, "Thao tác thích thất bại");

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

        public async void Receive(Guid parameter)
        {
            _diaryId = parameter;
            IsBusy = true;
            await LoadCommentsAsync();
            IsBusy = false;

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

        [RelayCommand]
        public async Task LoadMoreRepliesAsync(LoadMoreRepliesItem item)
        {
            var parent = Comments.FirstOrDefault(c => c.Id == item.ParentId);
            if (parent == null || parent.IsLoadingReplies)
                return;

            await LoadRepliesInternalAsync(parent, isFirstPage: false);
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

        private void RemoveReplies(Guid parentId)
        {
            var items = FlatComments
        .Where(x =>
            (x.Level == 1 && x.RootCommentId == parentId) ||
            x is LoadMoreRepliesItem lm && lm.ParentId == parentId)
        .ToList();


            foreach (var i in items)
                FlatComments.Remove(i);
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
                    new DiaryCommentFlatItem
                    {
                        Id = reply.Id,
                        Comment = reply,
                        Level = 1,
                        RootCommentId = parent.Id,
                        IsLiked = reply.IsLiked,
                        LikeCount = reply.LikeCount
                    });
            }

            // ⭐ Thêm LoadMore nếu còn
            if (parent.HasMoreReplies)
            {
                FlatComments.Insert(insertIndex,
                    new LoadMoreRepliesItem
                    {
                        ParentId = parent.Id,
                        Level = 1,
                        RootCommentId = parent.Id
                    });
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
                FlatComments.Insert(insertIndex++, new DiaryCommentFlatItem
                {
                    Id = reply.Id,
                    Comment = reply,
                    Level = 1,
                    RootCommentId = parent.Id,
                    IsLiked = reply.IsLiked,
                    LikeCount = reply.LikeCount
                });
            }
            if (parent.HasMoreReplies)
            {
                FlatComments.Insert(insertIndex, new LoadMoreRepliesItem
                {
                    ParentId = parent.Id,
                    Level = 1,
                    RootCommentId = parent.Id
                });
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
                FlatComments.Insert(insertIndex++, new DiaryCommentFlatItem
                {
                    Id = reply.Id,
                    Comment = reply,
                    Level = 1,
                    RootCommentId = parent.Id,
                    IsLiked = reply.IsLiked,
                    LikeCount = reply.LikeCount
                });
            }
            // Nếu hết replies thì xóa dòng LoadMore
            if (!parent.HasMoreReplies)
            {
                var loadMore = FlatComments.OfType<LoadMoreRepliesItem>().FirstOrDefault(x => x.ParentId == parent.Id);
                if (loadMore != null)
                    FlatComments.Remove(loadMore);
            }
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
