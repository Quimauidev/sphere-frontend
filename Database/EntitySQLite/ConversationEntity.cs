using SQLite;
using System;

namespace Sphere.Database.EntitySQLite
{
    public class ConversationEntity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public Guid PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAvatar { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public bool IsDeletedForCurrentUser { get; set; }
        public bool IsOnline { get; set; }
    }
}
