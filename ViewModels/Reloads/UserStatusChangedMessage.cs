using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels.Reloads
{
    // 🔹 Message: Một user cụ thể thay đổi trạng thái online/offline
    public class UserStatusChangedMessage : ValueChangedMessage<(Guid UserId, bool IsOnline)>
    {
        public Guid UserId => Value.UserId;
        public bool IsOnline => Value.IsOnline;
        public UserStatusChangedMessage(Guid userId, bool isOnline)
            : base((userId, isOnline)) { }
    }

    // 🔹 Message: Toàn bộ danh sách online hiện tại đã được tải (khi nhận từ server lần đầu)
    public record AllOnlineUsersLoadedMessage;
}
