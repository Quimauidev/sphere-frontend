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
        private bool isLiked;

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

        [RelayCommand]
        public async Task LoadCommentsAsync()
        {
            if (_isLoaded) return;
            _isLoaded = true;
            var res = await _diaryService.GetCommentAsync(_diaryId, 1, 20);
            if (!res.IsSuccess) return;

            Comments.Clear();
            foreach (var c in res.Data!)
                Comments.Add(c);
            BuildFlatComments();
        }
        private void BuildFlatComments()
        {
            FlatComments.Clear();

            foreach (var parent in Comments)
            {
                FlatComments.Add(new DiaryCommentFlatItem
                {
                    Id = parent.Id,
                    Comment = parent,
                    Level = 0,
                    RootCommentId = parent.Id
                });

                foreach (var reply in parent.Replies)
                {
                    FlatComments.Add(new DiaryCommentFlatItem
                    {
                        Id = reply.Id,
                        Comment = reply,
                        Level = 1,
                        RootCommentId = parent.Id
                    });
                }
            }
        }

        [RelayCommand]
        public async Task SendCommentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCommentContent))
                return;

            if (IsBusy) return;
            IsBusy = true;

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
                        
                        // 1️⃣ xác định comment gốc
                        var rootId = ReplyToComment!.ParentCommentId ?? ReplyToComment.Id;

                        var root = Comments.FirstOrDefault(c => c.Id == rootId);
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
                    NewCommentContent = string.Empty;
                    ReplyToComment = null;
                }
                else
                {
                    await ApiResponseHelper.ShowApiErrorsAsync(res, "Gửi bình luận thất bại");
                }
                KeyboardService.HideKeyboard();
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

        public void Receive(Guid parameter)
        {
            _diaryId = parameter;
            _ = LoadCommentsAsync();
        }
    }
}
