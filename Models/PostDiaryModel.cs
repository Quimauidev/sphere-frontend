using Sphere.Common.Constans;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Models
{
    public class PostDiaryModel
    {
        public string? Content { get; set; } // Nội dung
        public ObservableCollection<string> ImagePaths { get; set; } = [];
        public Privacy Privacy { get; set; } // Công khai / Bạn bè / Riêng tư
    }
}