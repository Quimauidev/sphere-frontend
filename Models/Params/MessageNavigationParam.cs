using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Models.Params
{
    public class MessageNavigationParam
    {
        public Guid ConversationId { get; set; }
        public UserDiaryModel Partner { get; set; } = default!;
        public string PartnerFullName { get; set; } = string.Empty;
        public string? PartnerAvatar { get; set; }
    }
}
