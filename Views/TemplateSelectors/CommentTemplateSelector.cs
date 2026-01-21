using Sphere.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Views.TemplateSelectors
{
    public class CommentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CommentTemplate { get; set; } = null!;
        public DataTemplate LoadMoreTemplate { get; set; } = null!;

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                LoadMoreRepliesItem => LoadMoreTemplate,
                _ => CommentTemplate
            };
        }
    }
}
