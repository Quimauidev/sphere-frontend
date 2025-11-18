using CommunityToolkit.Maui;
using FFImageLoading.Maui;
using Microsoft.Extensions.Logging;
using Sphere.Common.Constans;
using Sphere.Extensions;
using Sphere.Platforms.Android;
using Sphere.Platforms.Android.Handlers;
using Sphere.Services.IService;
using Sphere.Services.Service;
using Sphere.Views.Controls;
using Sphere.ViewModels;
using Android.Content.Res;
using Microsoft.Maui.Platform;
using Sphere.Database.ServiceSQLite;

namespace Sphere
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseFFImageLoading()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("materialdesignicons-webfont.ttf", "MaterialIcons");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<MediaStoreHelper>();
            builder.Services.AddHttpClient();
            builder.Services.RegisterServices();
            //builder.Services.AddSingleton<IMediaUploadService, MediaUploadService>();
            
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(GlideImage), typeof(GlideImageHandler));
            });
            builder.Services.AddTransient<AuthHandler>();
            builder.Services.AddHttpClient("AuthorizedClient", client =>
            {
                client.BaseAddress = new Uri("https://sphere-iqm8.onrender.com");
            })
            .AddHttpMessageHandler<AuthHandler>();
#if ANDROID
            Microsoft.Maui.Handlers.EditorHandler.Mapper.ModifyMapping("NoUnderline", (handler, view, _) =>
            {
                handler.PlatformView.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
            });
#endif

            return builder.Build();
        }
    }
}