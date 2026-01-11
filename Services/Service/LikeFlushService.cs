using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Services.Service
{
    public static class LikeFlushService
    {
        public static Func<Task>? Flush;

        public static async Task FlushAsync()
        {
            if (Flush != null)
                await Flush();
        }
    }

}
