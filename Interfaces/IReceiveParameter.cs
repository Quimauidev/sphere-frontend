using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Interfaces
{
    public interface IReceiveParameter<T>
    {
        void Receive(T param);
    }

}
