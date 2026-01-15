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
        //public Action<DiaryCommentUIModel>? ScrollToComment { get; set; }

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
        }

        // hàm này đúng chỉ 2 cấp bình luận
        private DiaryCommentUIModel? FindRootComment(Guid id)
        {
            return Comments.FirstOrDefault(c =>
                c.Id == id ||
                c.Replies.Any(r => r.Id == id)
            );
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
                        ScrollToIndex?.Invoke(0);
                    }
                    else
                    {
                        var replyTo = ReplyToComment!;

                        // xác định comment gốc
                        var rootId = replyTo.ParentCommentId ?? replyTo.Id;

                        var root = FindRootComment(rootId);
                        if (root != null)
                        {
                            root.Replies.Insert(0, res.Data!);
                        }
                        else
                        {
                            // fallback an toàn
                            Comments.Insert(0, res.Data!);
                            ScrollToIndex?.Invoke(0);
                        }
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
        public void Reply(DiaryCommentUIModel comment)
        {
            ReplyToComment = comment;
            NewCommentContent = string.Empty;
            RequestFocusCommentEditor?.Invoke();
            DiaryCommentUIModel? targetRoot;

            if (comment.ParentCommentId == null)
            {
                // reply comment cha
                targetRoot = comment;
            }
            else
            {
                // reply comment con → scroll CHA và scroll reply trong replies
                targetRoot = FindRootComment(comment.ParentCommentId.Value);
                
            }

            if (targetRoot != null)
            {
                var index = Comments.IndexOf(targetRoot);
                if (index >= 0)
                    ScrollToIndex?.Invoke(index);
            }

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
