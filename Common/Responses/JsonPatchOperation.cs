using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Responses
{
    internal class JsonPatchOperation
    {
        public string Op { get; set; } = "replace";
        public string? Path { get; set; }
        public object? Value { get; set; }
    }
}
