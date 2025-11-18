using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Reloads
{
    public record UserFollowChangedMessage(Guid UserId, bool IsFollowing);

}
