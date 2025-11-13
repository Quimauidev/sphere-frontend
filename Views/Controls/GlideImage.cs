using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Views.Controls
{
    public class GlideImage : View
    {
        public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(
            nameof(Source),
            typeof(string),
            typeof(GlideImage),
            default(string));

        public string? Source
        {
            get => (string?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
    }
}
