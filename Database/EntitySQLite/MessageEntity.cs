using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Database.EntitySQLite
{
    public class MessageEntity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }

        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsRead { get; set; }
        public bool IsRecalled { get; set; }

        public DateTime SentAt { get; set; }

        public int Status { get; set; }   // int để lưu enum
    }
}
