using CommunityToolkit.Mvvm.Messaging.Messages;
using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Reloads
{
    public class DiaryUpdatedMessage : ValueChangedMessage<DiaryModel>
    {
        public DiaryUpdatedMessage(DiaryModel value) : base(value) { }
    }

}
