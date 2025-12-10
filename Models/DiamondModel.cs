using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class DiamondModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public long Coins { get; set; }
        public bool IsSpecial { get; set; }
    }

    public class CreateDepositRequest
    {
        public long UserIdNumber { get; set; } // mã định danh dễ thao tác
        public Guid PackageId { get; set; }
    }
}
