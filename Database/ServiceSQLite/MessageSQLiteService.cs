using Sphere.Common.Constans;
using Sphere.Database.EntitySQLite;
using Sphere.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Database.ServiceSQLite
{
    public class MessageSQLiteService
    {
        private readonly SQLiteAsyncConnection _db;
        private const int MaxCountPerConversation = 200;

        public MessageSQLiteService(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task SaveMessagesAsync(IEnumerable<MessageEntity> messages)
        {
            await _db.RunInTransactionAsync(conn =>
            {
                foreach (var m in messages)
                    conn.InsertOrReplace(m);
            });

            // Trim mỗi conversation sau khi lưu
            var grouped = messages.GroupBy(m => m.ConversationId);
            foreach (var group in grouped)
            {
                await TrimMessagesAsync(group.Key);
            }
        }

        private async Task TrimMessagesAsync(Guid conversationId)
        {
            var total = await _db.Table<MessageEntity>()
                                 .Where(m => m.ConversationId == conversationId)
                                 .CountAsync();
            if (total <= MaxCountPerConversation) return;

            var toDelete = await _db.Table<MessageEntity>()
                                    .Where(m => m.ConversationId == conversationId)
                                    .OrderBy(m => m.SentAt)
                                    .Take(total - MaxCountPerConversation)
                                    .ToListAsync();

            foreach (var msg in toDelete)
                await _db.DeleteAsync(msg);
        }

        public async Task<IEnumerable<MessageEntity>> GetMessagesAsync(Guid conversationId, int skip, int take)
        {
            var page = await _db.Table<MessageEntity>()
                                .Where(m => m.ConversationId == conversationId)
                                .OrderByDescending(m => m.SentAt)
                                .Skip(skip)
                                .Take(take)
                                .ToListAsync();
            return page.OrderBy(m => m.SentAt);
        }

        public async Task ClearConversationAsync(Guid conversationId)
        {
            await _db.Table<MessageEntity>().DeleteAsync(m => m.ConversationId == conversationId);
        }

        public MessageModel MapEntityToModel(MessageEntity e, Guid myId) => new()
        {
            Id = e.Id,
            ConversationId = e.ConversationId,
            SenderId = e.SenderId,
            ReceiverId = e.ReceiverId,
            Content = e.Content,
            MediaUrl = e.MediaUrl,
            MediaType = e.MediaType,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            IsRead = e.IsRead,
            IsRecalled = e.IsRecalled,
            SentAt = e.SentAt,
            Status = (MessageStatus)e.Status,
            IsMine = e.SenderId == myId
        };

        public MessageEntity MapModelToEntity(MessageModel m) => new()
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            MediaUrl = m.MediaUrl,
            MediaType = m.MediaType,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            IsRead = m.IsRead,
            IsRecalled = m.IsRecalled,
            SentAt = m.SentAt,
            Status = (int)m.Status
        };
    }

}
