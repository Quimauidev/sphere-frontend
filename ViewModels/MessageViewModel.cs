using Android.Icu.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sphere;
using Sphere.Common.Constans;
using Sphere.Common.Responses;
using Sphere.Database.ServiceSQLite;
using Sphere.Models;
using Sphere.Services.IService;
using Sphere.Services.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sphere.Models.Request;
using static System.Net.Mime.MediaTypeNames;
using Sphere.Database.EntitySQLite;

namespace Sphere.ViewModels
{
    public partial class MessageViewModel : ObservableObject
    {
        private readonly IConversationService _conversationService;
        private readonly IUserSessionService _userSessionService;
        private readonly MessageHubService _hubService;
        private readonly MessageSQLiteService _localMessageDb;

        public ObservableCollection<MessageModel> Messages { get; private set; } = new();

        [ObservableProperty]
        private string? partnerAvatar;

        [ObservableProperty]
        private string? partnerFullName;

        [ObservableProperty]
        private Guid conversationId;

        [ObservableProperty]
        private string? currentMessage;

        private int _skip = 0;
        private const int PageSize = 50;

        private Guid _previousConversationId = Guid.Empty;

        // Hub event delegates
        private Action<MessageModel>? _onMessageReceivedHandler;
        private Action<Guid>? _onMessageSentConfirmedHandler;
        private Action<Guid>? _onMessageDeliveredHandler;
        private Action<Guid>? _onMessageSeenHandler;
        private Action<Guid, string>? _onMessagesMarkedAsReadHandler;
        private bool _isLoadingOlder;

        public Action? ScrollToLastMessage { get; set; }
        public Action<MessageModel>? ScrollToMessage { get; set; }

        public MessageViewModel(
            IConversationService conversationService,
            IUserSessionService userSessionService,
            MessageHubService hubService,
            MessageSQLiteService localMessageDb)
        {
            _conversationService = conversationService;
            _userSessionService = userSessionService;
            _hubService = hubService;
            _localMessageDb = localMessageDb;
        }

        public void SetPartner(UserDiaryModel partner)
        {
            PartnerFullName = partner.FullName;
            PartnerAvatar = !string.IsNullOrWhiteSpace(partner.AvatarUrl)
                ? partner.AvatarUrl
                : partner.Gender == Gender.Female ? "woman.png" : "man.png";
        }

        partial void OnConversationIdChanged(Guid value)
        {
            // Leave previous conversation
            if (_previousConversationId != Guid.Empty && _previousConversationId != value)
            {
                UnregisterHubEvents();
                _ = _hubService.LeaveConversation(_previousConversationId);
            }

            _previousConversationId = value;
            RegisterHubEvents();
            _ = LoadMessagesAsync(value);
        }

        private void RegisterHubEvents()
        {
            UnregisterHubEvents();

            _onMessageReceivedHandler = msg => { _ = HandleMessageReceivedAsync(msg); };
            _hubService.OnMessageReceived += _onMessageReceivedHandler;

            _onMessageSentConfirmedHandler = id => { _ = HandleMessageSentConfirmedAsync(id); };
            _hubService.OnMessageSentConfirmed += _onMessageSentConfirmedHandler;

            _onMessageDeliveredHandler = id => { _ = HandleMessageDeliveredAsync(id); };
            _hubService.OnMessageDelivered += _onMessageDeliveredHandler;

            _onMessageSeenHandler = id => { _ = HandleMessageSeenAsync(id); };
            _hubService.OnMessageSeen += _onMessageSeenHandler;

            _onMessagesMarkedAsReadHandler = (convId, userId) =>
            {
                if (convId == ConversationId)
                {
                    foreach (var m in Messages.Where(x => !x.IsMine))
                        m.IsRead = true;
                }
            };
            _hubService.OnMessagesMarkedAsRead += _onMessagesMarkedAsReadHandler;
        }

        private void UnregisterHubEvents()
        {
            if (_onMessageReceivedHandler != null)
                _hubService.OnMessageReceived -= _onMessageReceivedHandler;
            if (_onMessageSentConfirmedHandler != null)
                _hubService.OnMessageSentConfirmed -= _onMessageSentConfirmedHandler;
            if (_onMessageDeliveredHandler != null)
                _hubService.OnMessageDelivered -= _onMessageDeliveredHandler;
            if (_onMessageSeenHandler != null)
                _hubService.OnMessageSeen -= _onMessageSeenHandler;
            if (_onMessagesMarkedAsReadHandler != null)
                _hubService.OnMessagesMarkedAsRead -= _onMessagesMarkedAsReadHandler;

            _onMessageReceivedHandler = null;
            _onMessageSentConfirmedHandler = null;
            _onMessageDeliveredHandler = null;
            _onMessageSeenHandler = null;
            _onMessagesMarkedAsReadHandler = null;
        }

        private async Task SaveLocalAsync(MessageModel message)
        {
            var entity = _localMessageDb.MapModelToEntity(message);
            await _localMessageDb.SaveMessagesAsync(new[] { entity });
        }

        private async Task HandleMessageReceivedAsync(MessageModel msg)
        {
            if (msg.ConversationId != ConversationId) return;

            var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;
            msg.IsMine = msg.SenderId == currentUserId;

            // Find by Id first
            var existingById = Messages.FirstOrDefault(m => m.Id == msg.Id);

            // If not found, try to find matching temp message (same content, mine, sending/failed)
            var matchingTemp = Messages.FirstOrDefault(m => m.IsMine && (m.Status == MessageStatus.Sending || m.Status == MessageStatus.Failed) && m.Content == msg.Content && m.SenderId == msg.SenderId);

            if (existingById != null)
            {
                existingById.Content = msg.Content;
                existingById.SentAt = msg.SentAt;
                existingById.Status = msg.Status;
                existingById.IsRead = msg.IsRead;
                existingById.IsRecalled = msg.IsRecalled;
                await SaveLocalAsync(existingById);
            }
            else if (matchingTemp != null)
            {
                matchingTemp.Id = msg.Id;
                matchingTemp.SentAt = msg.SentAt;
                matchingTemp.Status = msg.Status;
                matchingTemp.IsMine = msg.IsMine;
                matchingTemp.IsRead = msg.IsRead;
                matchingTemp.IsRecalled = msg.IsRecalled;
                matchingTemp.Content = msg.Content;
                await SaveLocalAsync(matchingTemp);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => Messages.Add(msg));
                await SaveLocalAsync(msg);
                ScrollToLastMessage?.Invoke();
            }

            if (!msg.IsMine) // chỉ gọi seen khi là tin của người khác
            {
                try
                {
                    await _hubService.NotifySeenAsync(msg.Id, msg.ConversationId);
                }
                catch { }
            }
            UpdateLastMessageFlag();
            UpdateMessageStatusIcons();
        }

        private async Task HandleMessageSentConfirmedAsync(Guid messageId)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.Status = MessageStatus.Sent;
                await SaveLocalAsync(msg);
                UpdateMessageStatusIcons();
            }
        }

        private async Task HandleMessageDeliveredAsync(Guid messageId)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.Status = MessageStatus.Delivered;
                await SaveLocalAsync(msg);
                UpdateMessageStatusIcons();
            }
        }

        private async Task HandleMessageSeenAsync(Guid messageId)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.Status = MessageStatus.Seen;
                msg.IsRead = true;
                await SaveLocalAsync(msg);
                UpdateMessageStatusIcons();
            }
        }

        private async Task LoadMessagesAsync(Guid conversationId)
        {
            if (conversationId == Guid.Empty) return;

            var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;

            // 1) Load from local DB
            var localEntities = await _localMessageDb.GetMessagesAsync(conversationId, 0, PageSize);
            var localModels = localEntities.Select(e => _localMessageDb.MapEntityToModel(e, currentUserId))
                                           .OrderBy(m => m.SentAt).ToList();

            // If some messages are still in Sending state (app died), mark them Failed
            var changed = new List<MessageEntity>();
            foreach (var lm in localModels.Where(x => x.IsMine && x.Status == MessageStatus.Sending).ToList())
            {
                lm.Status = MessageStatus.Failed;
                changed.Add(_localMessageDb.MapModelToEntity(lm));
            }
            if (changed.Any())
            {
                await _localMessageDb.SaveMessagesAsync(changed);
            }

            Messages = new ObservableCollection<MessageModel>(localModels);
            OnPropertyChanged(nameof(Messages));

           
            UpdateLastMessageFlag();
            UpdateMessageStatusIcons();
            ScrollToLastMessage?.Invoke();

            // 2) Join hub to receive realtime updates
            await _hubService.JoinConversation(conversationId);

            // 3) Load from server (latest)
            var response = await _conversationService.GetLatestMessagesAsync(conversationId, 0, PageSize);
            if (!response.IsSuccess || response.Data == null) return;

            var entities = response.Data.Select(m => _localMessageDb.MapModelToEntity(m)).ToList();
            await _localMessageDb.SaveMessagesAsync(entities);

            await MergeServerMessagesAsync(response.Data, currentUserId);
            ScrollToLastMessage?.Invoke();
        }

        private async Task MergeServerMessagesAsync(IEnumerable<MessageModel> serverMessages, Guid currentUserId)
        {
            var localById = Messages.ToDictionary(m => m.Id, m => m);
            var toSave = new List<MessageEntity>();

            foreach (var serverMsg in serverMessages)
            {
                serverMsg.IsMine = serverMsg.SenderId == currentUserId;

                if (localById.TryGetValue(serverMsg.Id, out var local))
                {
                    if (local.Status != MessageStatus.Failed)
                    {
                        local.Content = serverMsg.Content;
                        local.SentAt = serverMsg.SentAt;
                        local.Status = serverMsg.Status;
                        local.IsRead = serverMsg.IsRead;
                        local.IsRecalled = serverMsg.IsRecalled;
                        toSave.Add(_localMessageDb.MapModelToEntity(local));
                    }
                }
                else
                {
                    var temp = Messages.FirstOrDefault(m => m.IsMine && (m.Status == MessageStatus.Sending || m.Status == MessageStatus.Failed) && m.Content == serverMsg.Content && m.SenderId == serverMsg.SenderId);
                    if (temp != null)
                    {
                        temp.Id = serverMsg.Id;
                        temp.SentAt = serverMsg.SentAt;
                        temp.Status = serverMsg.Status;
                        temp.IsMine = serverMsg.IsMine;
                        temp.IsRead = serverMsg.IsRead;
                        temp.IsRecalled = serverMsg.IsRecalled;
                        temp.Content = serverMsg.Content;
                        toSave.Add(_localMessageDb.MapModelToEntity(temp));
                    }
                    else
                    {
                        Messages.Add(serverMsg);
                        toSave.Add(_localMessageDb.MapModelToEntity(serverMsg));
                    }
                }
            }

            if (toSave.Any())
                await _localMessageDb.SaveMessagesAsync(toSave);

            UpdateLastMessageFlag();
            UpdateMessageStatusIcons();
        }

        [RelayCommand]
        public async Task LoadMoreMessagesAsync()
        {
            if (_isLoadingOlder) return;
            _isLoadingOlder = true;

            try
            {
                var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;

                // 1️⃣ Load thêm từ SQLite trước (offline)
                var localMore = await _localMessageDb.GetMessagesAsync(
                    ConversationId,
                    _skip,
                    PageSize
                );

                foreach (var msgEntity in localMore.OrderBy(m => m.SentAt))
                {
                    var model = _localMessageDb.MapEntityToModel(msgEntity, currentUserId);
                    if (!Messages.Any(x => x.Id == model.Id))
                        Messages.Insert(0, model);
                }

                // 2️⃣ Nếu có mạng → load từ API (sync tin mới nhất)
                if (Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet))
                {
                    var response = await _conversationService.GetLatestMessagesAsync(
                        ConversationId,
                        _skip,
                        PageSize
                    );

                    if (response.IsSuccess && response.Data != null && response.Data.Any())
                    {
                        var toSave = new List<MessageEntity>();

                        foreach (var serverMsg in response.Data)
                        {
                            serverMsg.IsMine = serverMsg.SenderId == currentUserId;

                            // Check duplicate trong Messages
                            if (!Messages.Any(x => x.Id == serverMsg.Id))
                                Messages.Insert(0, serverMsg);

                            // Save server message vào SQLite
                            toSave.Add(_localMessageDb.MapModelToEntity(serverMsg));
                        }

                        if (toSave.Any())
                            await _localMessageDb.SaveMessagesAsync(toSave);
                    }
                }

                // 3️⃣ Cập nhật skip và UI
                _skip += PageSize;
                UpdateLastMessageFlag();
                UpdateMessageStatusIcons();
                if (Messages.Count > 0 && ScrollToMessage != null)
                    ScrollToMessage(Messages[0]);
            }
            finally
            {
                _isLoadingOlder = false;
            }
        }


        [RelayCommand]
        private async Task SendMessage()
        {
            if (ConversationId == Guid.Empty || string.IsNullOrWhiteSpace(CurrentMessage)) return;

            var currentUserId = _userSessionService.CurrentUser?.UserDTO?.Id ?? Guid.Empty;

            var tempMessage = new MessageModel
            {
                Id = Guid.NewGuid(),
                ConversationId = ConversationId,
                SenderId = currentUserId,
                ReceiverId = Guid.Empty,
                Content = CurrentMessage,
                SentAt = DateTime.UtcNow,
                IsMine = true,
                Status = MessageStatus.Sending
            };

            Messages.Add(tempMessage);
            UpdateLastMessageFlag();
            ScrollToLastMessage?.Invoke();

            MainThread.BeginInvokeOnMainThread(() => CurrentMessage = string.Empty);

            await SaveLocalAsync(tempMessage);

            if (!Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet))
            {
                tempMessage.Status = MessageStatus.Failed;
                await SaveLocalAsync(tempMessage);
                UpdateMessageStatusIcons();
                return;
            }

            try
            {
                await _hubService.SendMessage(ConversationId, tempMessage.Content ?? string.Empty, tempMessage.Id);
            }
            catch
            {
                tempMessage.Status = MessageStatus.Failed;
                await SaveLocalAsync(tempMessage);
                UpdateMessageStatusIcons();
            }
        }

        [RelayCommand]
        private async Task RetrySendMessage(MessageModel message)
        {
            if (message == null || message.Status != MessageStatus.Failed) return;

            message.Status = MessageStatus.Sending;
            await SaveLocalAsync(message);
            UpdateMessageStatusIcons();

            if (!Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet))
            {
                message.Status = MessageStatus.Failed;
                await SaveLocalAsync(message);
                UpdateMessageStatusIcons();
                return;
            }

            try
            {
                await _hubService.SendMessage(message.ConversationId, message.Content ?? string.Empty, message.Id);
            }
            catch
            {
                message.Status = MessageStatus.Failed;
                await SaveLocalAsync(message);
                UpdateMessageStatusIcons();
            }
        }

        private void UpdateLastMessageFlag()
        {
            if (!Messages.Any()) return;

            for (int i = 0; i < Messages.Count - 1; i++)
                Messages[i].IsLastMessage = false;

            Messages[^1].IsLastMessage = true;
        }

        private void UpdateMessageStatusIcons()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var myMessages = Messages.Where(m => m.IsMine).ToList();
                if (!myMessages.Any()) return;

                var lastNormal = myMessages.Where(m => m.Status != MessageStatus.Sending && m.Status != MessageStatus.Failed).LastOrDefault();

                foreach (var m in myMessages)
                {
                    bool newShow = m.Status == MessageStatus.Failed || (lastNormal != null && m == lastNormal);
                    if (m.ShowStatusIcon != newShow) m.ShowStatusIcon = newShow;
                }
            });
        }

        



        [RelayCommand]
        private void OpenGallery() => App.Current!.MainPage!.DisplayAlert("Gallery", "Open gallery", "OK");

        [RelayCommand]
        private void SendLocation() => App.Current!.MainPage!.DisplayAlert("Location", "Send location", "OK");

        [RelayCommand]
        private void Attach() => App.Current!.MainPage!.DisplayAlert("Attach", "Open attachments", "OK");
    }

}
