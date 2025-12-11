using Sphere.Common.Constans;
using Sphere.Database.ServiceSQLite;
using Sphere.ViewModels;
using Sphere.Views.Pages;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sphere.Extensions
{
    public static class ServiceRegistration
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sphere.db3");
            var connection = new SQLiteAsyncConnection(dbPath);
            services.AddSingleton(connection);
            // đăng ký BaseSQLiteService
            services.AddSingleton<BaseSQLiteService>();
            // đăng ký các service SQLite
            services.AddSingleton<MessageSQLiteService>();
            services.AddSingleton<ConversationSQLiteService>();

            // 🔹 Đăng ký HubConfig trước các service cần nó
            services.AddSingleton(new HubConfig
            {
                HubUrl = "https://sphere-iqm8.onrender.com/chathub"
            });

            // 🔹 Đăng ký tất cả Pages vào DI
            foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ContentPage)) && !t.IsAbstract))
            {
                if (IsSingletonPage(type))
                    services.AddSingleton(type);
                else
                    services.AddTransient(type);
            }

            // 🔹 Đăng ký tất cả ViewModel vào DI
            foreach (var type in assembly.GetTypes().Where(t => t.Name.EndsWith("ViewModel") && t.IsClass && !t.IsAbstract))
            {
                services.AddTransient(type);
            }

            // 🔹 Đăng ký tất cả Service vào DI (Hỗ trợ nhiều Interface)
            foreach (var type in assembly.GetTypes().Where(t => t.Name.EndsWith("Service") && t.IsClass && !t.IsAbstract))
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Length > 0)
                {
                    foreach (var interfaceType in interfaces)
                    {
                        services.AddSingleton(interfaceType, type);
                    }
                }
                else
                {
                    // Đăng ký luôn class nếu không có interface
                    services.AddSingleton(type);
                }
            }
        }

        // Xác định Page cần giữ state
        private static bool IsSingletonPage(Type type)
        {
            return type == typeof(NationwidePage);
        }
    }
}