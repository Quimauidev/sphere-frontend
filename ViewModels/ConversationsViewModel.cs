using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Models;
using Sphere.Services.IService;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere.ViewModels
{
    public partial class ConversationsViewModel : ObservableObject
    {
        private readonly IConversationService _conversationService;

        public ObservableCollection<ConversationModel> Conversations { get; } = [];

        [ObservableProperty]
        private ConversationModel? selectedConversation;

        public ConversationsViewModel(IConversationService conversationService)
        {
            _conversationService = conversationService;
            _ = LoadAsync();
        }

        partial void OnSelectedConversationChanged(ConversationModel? value)
        {
            if (value == null) return;
            _ = OnConversationSelectedAsync(value);
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                var resp = await _conversationService.GetConversationsAsync();
                if (resp.IsSuccess && resp.Data != null)
                {
                    Conversations.Clear();

                    var sorted = resp.Data
                        .Where(x => !x.IsDeletedForCurrentUser)
                        .OrderByDescending(x => x.LastUpdatedAt);

                    foreach (var dto in sorted)
                    {
                        Conversations.Add(new ConversationModel
                        {
                            Id = dto.Id,
                            PartnerName = dto.PartnerName,
                            PartnerAvatar = dto.PartnerAvatar,
                            LastMessage = dto.LastMessage,      
                            LastUpdatedAt = dto.LastUpdatedAt,
                            IsDeletedForCurrentUser = dto.IsDeletedForCurrentUser,
                            IsOnline = false
                        });
                    }
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
        }

        private async Task OnConversationSelectedAsync(ConversationModel conversation)
        {
            if (conversation == null) return;

            await Shell.Current.GoToAsync($"MessagePage?ConversationId={conversation.Id}");

            // reset để chọn lại được item cũ lần sau
            SelectedConversation = null;
        }
    }
}
