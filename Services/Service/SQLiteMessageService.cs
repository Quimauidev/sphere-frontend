using Sphere.Common.Constans;
using Sphere.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    public class SQLiteMessageService
    {
        private readonly SQLiteAsyncConnection _db;

        public SQLiteMessageService(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<MessageEntity>().Wait();
            //_db.CreateTableAsync<ConversationCacheEntity>().Wait();
        }

        // Lưu batch 50–100 tin
        public async Task SaveMessagesAsync(IEnumerable<MessageEntity> messages)
        {
            foreach (var m in messages)
            {
                await _db.InsertOrReplaceAsync(m);
            }
        }

        // Lấy tin theo ConversationId (order đúng)
        public async Task<List<MessageEntity>> GetMessagesAsync(Guid conversationId, int limit = 200)
        {
            return await _db.Table<MessageEntity>()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task ClearConversationAsync(Guid conversationId)
        {
            await _db.Table<MessageEntity>()
                .DeleteAsync(m => m.ConversationId == conversationId);
        }
    

    public MessageModel MapEntityToModel(MessageEntity e, Guid myId)
        {
            return new MessageModel
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
        }

        public MessageEntity MapModelToEntity(MessageModel m)
        {
            return new MessageEntity
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
}
