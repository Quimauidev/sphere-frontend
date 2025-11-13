using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public partial class ImageItem : ObservableObject
    {
        public string? ContentUriString { get; set; }

        [ObservableProperty]
        public bool isSelected;

        [ObservableProperty]
        public int orderNumber;  // 0 = chưa chọn
    }

}
