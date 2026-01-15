using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Interfaces
{
    public interface IModalParameterReceiver<T>
    {
        void Receive(T parameter);
    }

}
