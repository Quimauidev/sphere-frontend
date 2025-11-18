using Sphere.Database.EntitySQLite;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Database.ServiceSQLite
{
    // Base service quản lý DB chung
    public class BaseSQLiteService
    {
        protected readonly SQLiteAsyncConnection _db;

        public BaseSQLiteService(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitAsync()
        {
            // Tạo tất cả table 1 lần duy nhất
            await _db.CreateTableAsync<MessageEntity>();
            await _db.CreateTableAsync<ConversationEntity>();
            // Thêm table khác nếu cần
        }
    }

}
