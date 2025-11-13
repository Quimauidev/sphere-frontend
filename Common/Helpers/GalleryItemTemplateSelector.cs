using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Common.Helpers
{
    public class GalleryItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ImageTemplate { get; set; }
        public DataTemplate? AddButtonTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            return item is string s && s == "+" ? AddButtonTemplate : ImageTemplate;
        }
    }

}
