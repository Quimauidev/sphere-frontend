using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Database.EntitySQLite;
using Sphere.Database.ServiceSQLite;
using Sphere.Models;
using Sphere.Models.Params;
using Sphere.Services.IService;
using Sphere.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Android.Icu.Util.LocaleData;

namespace Sphere.ViewModels
{
    public partial class ConversationsViewModel : ObservableObject
    {
        private readonly IConversationService _conversationService;
        private readonly ConversationSQLiteService _localConversationDb;
        private readonly IShellNavigationService _nv;
        private readonly ApiResponseHelper _res;
        private readonly IAppNavigationService _anv;
        public ObservableCollection<ConversationModel> Conversations { get; } = [];

        [ObservableProperty]
        private ConversationModel? selectedConversation;

        [ObservableProperty]
        private bool isLoading;

        public ConversationsViewModel(IConversationService conversationService, ConversationSQLiteService localConversationDb, IShellNavigationService nv, ApiResponseHelper res, IAppNavigationService anv)
        {
            _conversationService = conversationService;
            _localConversationDb = localConversationDb; 
            _nv = nv;
            _res = res;
            _anv = anv;
            _ = LoadAsync();
        }

        partial void OnSelectedConversationChanged(ConversationModel? value)
        {
            if (value == null) return;
            _ = OnConversationSelectedAsync(value);
        }
        private int CurrentPage = 1;
        private const int PageSize = 50;
        [RelayCommand]
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                // 🔹 1. Load từ SQLite trước
                var localList = await _localConversationDb.GetConversationsAsync(CurrentPage,PageSize);
                if (CurrentPage == 1)
                    Conversations.Clear();
              
                foreach (var entity in localList)
                    Conversations.Add(_localConversationDb.MapEntityToModel(entity));
                

                // 🔹 2. Đồng bộ với API page 100
                var resp = await _conversationService.GetConversationsAsync(CurrentPage,PageSize);
                if (resp.IsSuccess && resp.Data != null)
                {

                    foreach (var dto in resp.Data)
                    {
                        var entity = _localConversationDb.MapModelToEntity(dto);
                        await _localConversationDb.SaveOrUpdateConversationAsync(entity);

                    }
                    // 🔹 Reload SQLite sau khi sync để UI luôn là dữ liệu mới nhất
                    var updatedList = await _localConversationDb.GetConversationsAsync(CurrentPage,PageSize);
                    if (CurrentPage == 1)
                        Conversations.Clear();
                    foreach (var entity in updatedList)
                        Conversations.Add(_localConversationDb.MapEntityToModel(entity));
                }
                else
                {
                    await _res.ShowApiErrorsAsync(resp, "Không thể tải cuộc trò chuyện");
                }
            }
            catch (Exception ex)
            {
                await _anv.DisplayAlertAsync("Lỗi", $"Đã có lỗi xảy ra {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ClearConversationsAsync()
        {
            Conversations.Clear();
            CurrentPage = 1;
            await _localConversationDb.ClearAllConversationsAsync();
        }

        private async Task OnConversationSelectedAsync(ConversationModel conversation)
        {
            if (conversation == null) return;

            // Resolve page từ DI
            await _nv.PushModalAsync<MessagePage, MessageNavigationParam>( new MessageNavigationParam { ConversationId = conversation.Id, PartnerFullName = conversation.PartnerName!, PartnerAvatar = conversation.PartnerAvatar });
            // reset để chọn lại được item cũ lần sau
            SelectedConversation = null;
        }

        [RelayCommand]
        public async Task LoadMoreAsync()
        {
            if (IsLoading) return;

            CurrentPage++;
            await LoadAsync();
        }

    }
}