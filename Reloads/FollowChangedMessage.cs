using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere.Reloads
{
    public class FollowChangedMessage : ValueChangedMessage<Guid>
    {
        public FollowChangedMessage(Guid userId) : base(userId) { }
    }
}
