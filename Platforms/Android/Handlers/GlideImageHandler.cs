using Android.Content;
using Android.Widget;
using Bumptech.Glide;
using Microsoft.Maui.Handlers;
using Sphere.Views.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Platforms.Android.Handlers
{
    public partial class GlideImageHandler : ViewHandler<GlideImage, ImageView>
    {
        public static IPropertyMapper<GlideImage, GlideImageHandler> Mapper = new PropertyMapper<GlideImage, GlideImageHandler>(ViewHandler.ViewMapper)
        {
            [nameof(GlideImage.Source)] = MapSource
        };

        public GlideImageHandler() : base(Mapper)
        {
        }
        protected override ImageView CreatePlatformView()
        {
            return new ImageView(Context);
        }

        protected override void ConnectHandler(ImageView platformView)
        {
            base.ConnectHandler(platformView);
            UpdateSource();
        }

        public static void MapSource(GlideImageHandler handler, GlideImage view)
        {
            handler.UpdateSource();
        }

        void UpdateSource()
        {
            if (string.IsNullOrEmpty(VirtualView?.Source))
                return;

            Glide.With(Context)
                 .Load(VirtualView.Source)
                 .CenterCrop()
                 .Into(PlatformView);
        }
    }
}
