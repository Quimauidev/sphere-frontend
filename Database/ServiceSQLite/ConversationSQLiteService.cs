using Sphere.Database.EntitySQLite;
using Sphere.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere.Database.ServiceSQLite
{
    public class ConversationSQLiteService
    {
        private readonly SQLiteAsyncConnection _db;
        private const int MaxCount = 200;

        public ConversationSQLiteService(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task SaveOrUpdateConversationAsync(ConversationEntity entity)
        {
            var existing = await _db.FindAsync<ConversationEntity>(entity.Id);
            if (existing != null)
            {
                existing.PartnerName = entity.PartnerName;
                existing.PartnerAvatar = entity.PartnerAvatar;
                existing.LastMessage = entity.LastMessage;
                existing.LastUpdatedAt = entity.LastUpdatedAt;
                existing.IsDeletedForCurrentUser = entity.IsDeletedForCurrentUser;
                existing.IsOnline = entity.IsOnline;
                await _db.UpdateAsync(existing);
            }
            else
            {
                await _db.InsertAsync(entity);
            }

            await TrimConversationsAsync();
        }

        private async Task TrimConversationsAsync()
        {
            var total = await _db.Table<ConversationEntity>().CountAsync();
            if (total <= MaxCount) return;

            var toDelete = await _db.Table<ConversationEntity>()
                                    .OrderBy(c => c.LastUpdatedAt)
                                    .Take(total - MaxCount)
                                    .ToListAsync();
            foreach (var conv in toDelete)
                await _db.DeleteAsync(conv);
        }

        public async Task<IEnumerable<ConversationEntity>> GetConversationsAsync(int page, int pageSize)
        {
            return await _db.Table<ConversationEntity>()
                            .OrderByDescending(c => c.LastUpdatedAt)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();
        }

        public async Task DeleteConversationAsync(Guid id) => await _db.DeleteAsync<ConversationEntity>(id);
        public async Task ClearAllConversationsAsync() => await _db.DeleteAllAsync<ConversationEntity>();
        public ConversationModel MapEntityToModel(ConversationEntity e)
        {
            return new ConversationModel
            {
                Id = e.Id,
                PartnerName = e.PartnerName,
                PartnerAvatar = e.PartnerAvatar,
                LastMessage = e.LastMessage,
                LastUpdatedAt = e.LastUpdatedAt,
                IsDeletedForCurrentUser = e.IsDeletedForCurrentUser,
                IsOnline = e.IsOnline
            };
        }

        public ConversationEntity MapModelToEntity(ConversationModel m)
        {
            return new ConversationEntity
            {
                Id = m.Id,
                PartnerName = m.PartnerName!,
                PartnerAvatar = m.PartnerAvatar!,
                LastMessage = m.LastMessage,
                LastUpdatedAt = m.LastUpdatedAt,
                IsDeletedForCurrentUser = m.IsDeletedForCurrentUser,
                IsOnline = m.IsOnline
            };
        }

    }

}
