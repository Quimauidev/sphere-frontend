using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Database.EntitySQLite;
using Sphere.Database.ServiceSQLite;
using Sphere.Models;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly ConversationSQLiteService _localConversationDb;

        public ObservableCollection<ConversationModel> Conversations { get; } = [];

        [ObservableProperty]
        private ConversationModel? selectedConversation;

        [ObservableProperty]
        private bool isLoading;

        public ConversationsViewModel(IConversationService conversationService, IServiceProvider serviceProvider, ConversationSQLiteService localConversationDb)
        {
            _conversationService = conversationService;
            _serviceProvider = serviceProvider;
            _localConversationDb = localConversationDb; 
            _ = LoadAsync();
        }

        partial void OnSelectedConversationChanged(ConversationModel? value)
        {
            if (value == null) return;
            _ = OnConversationSelectedAsync(value);
        }
        private int CurrentPage = 1;
        private const int PageSize = 100;
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
                    await ApiResponseHelper.ShowApiErrorsAsync(resp, "Không thể tải cuộc trò chuyện");
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Lỗi", ex.Message, "OK");
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
            var page = _serviceProvider.GetRequiredService<MessagePage>();

            // Gán ConversationId → tự chạy OnConversationIdChanged
            if (page.BindingContext is MessageViewModel vm)
            {
                // Gán thông tin user để hiện lên header
                vm.PartnerFullName = conversation.PartnerName;
                vm.PartnerAvatar = conversation.PartnerAvatar;
                vm.ConversationId = conversation.Id;
            }

            // Điều hướng
            await Application.Current!.MainPage!.Navigation.PushModalAsync(page);

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