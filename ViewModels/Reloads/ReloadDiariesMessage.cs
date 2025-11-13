using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.ViewModels.Reloads
{
    public class ReloadDiariesMessage : ValueChangedMessage<bool>
    {
        public ReloadDiariesMessage(bool value) : base(value) { }
    }
}
