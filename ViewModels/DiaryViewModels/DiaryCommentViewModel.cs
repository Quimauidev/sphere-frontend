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

                if (parent.Replies == null)
                    continue;

                foreach (var reply in parent.Replies)
                    FlatComments.Add(CreateOrReuse(reply, 1, parent.Id, cache));
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

                        // 3️⃣ thêm vào Replies của root
                        root.Replies.Insert(0, res.Data!);

                    }
                    BuildFlatComments();
                    var flatItem = FlatComments.FirstOrDefault(x => x.Id == res.Data!.Id);
                    if (flatItem != null)
                    {
                        ScrollToFlatItem?.Invoke(flatItem);
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

            // 1️⃣ Nếu đang mở → đóng
            if (parent.IsRepliesExpanded)
            {
                parent.IsRepliesExpanded = false;

                var itemsToRemove = FlatComments
                .Where(x => x.Level == 1 && x.RootCommentId == parent.Id)
                .ToList();

                foreach (var i in itemsToRemove)
                    FlatComments.Remove(i);


                return;
            }

            if (parent.Replies != null && parent.Replies.Any())
            {
                if (!FlatComments.Any(x => x.Level == 1 && x.RootCommentId == parent.Id))
                {
                    InsertReplies(parent);
                }

                parent.IsRepliesExpanded = true;
                ScrollToLastReply(parent); // ⭐ THÊM
                return;
            }


            // 3️⃣ Load từ API
            parent.IsLoadingReplies = true;

            var res = await _diaryService.GetRepliesAsync(parent.Id, 1, 10);
            parent.IsLoadingReplies = false;

            if (!res.IsSuccess)
                return;

            parent.Replies = new ObservableCollection<DiaryCommentUIModel>(res.Data!);

            parent.IsRepliesExpanded = true;

            InsertReplies(parent);
            ScrollToLastReply(parent); // ⭐ THÊM
        }
        private void InsertReplies(DiaryCommentUIModel parent)
        {
            var cache = FlatComments.ToDictionary(x => x.Id);

            var parentIndex = FlatComments
                .Select((x, i) => new { x, i })
                .First(x => x.x.Id == parent.Id)
                .i;

            var insertIndex = parentIndex + 1;

            foreach (var reply in parent.Replies)
            {
                if (cache.TryGetValue(reply.Id, out var existing))
                {
                    FlatComments.Insert(insertIndex++, existing);
                }
                else
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
            }
        }
        private void ScrollToLastReply(DiaryCommentUIModel parent)
        {
            var lastReply = parent.Replies?.LastOrDefault();
            if (lastReply == null)
                return;

            var flatItem = FlatComments
                .FirstOrDefault(x => x.Id == lastReply.Id);

            if (flatItem != null)
                ScrollToFlatItem?.Invoke(flatItem);
        }

    }
}
