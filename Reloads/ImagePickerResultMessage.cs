using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Reloads
{
    public class ImagePickerResultMessage : ValueChangedMessage<List<string>>
    {
        public ImagePickerResultMessage(List<string> value) : base(value) { }
    }

}
