using MedicinesTracker.Services;
using MedicinesTracker.ViewModels;
using CommunityToolkit.Maui;
using MedicinesTracker.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Extensions.Configuration;   
using Microsoft.Extensions.DependencyInjection;
using MedicinesTracker.Repository;
using MedicinesTracker.Interface;

namespace MedicinesTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
                    handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Загрузка конфигурации из appsettings.json
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // Получение строки подключения
            var config = builder.Configuration;
            string? connectionString = config.GetConnectionString("Default");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Строка подключения не найдена в appsettings.json. Проверьте секцию ConnectionStrings:Default.");
            }

            builder.Services.AddSingleton<DBHandler>(
                sp => new DBHandler(connectionString));

            builder.Services.AddSingleton<IMedicineRepository, MedicineRepository>();
            builder.Services.AddSingleton<IUnitRepository, UnitRepository>();
            builder.Services.AddSingleton<IRecipientRepository, RecipientRepository>();
            builder.Services.AddSingleton<IMethodAdmissionRepository, MethodAdmissionRepository>();

            builder.Services.AddSingleton<MedicineService>();

            builder.Services.AddSingleton<AppShellVM>();
            builder.Services.AddSingleton<MedicineListVM>();
            builder.Services.AddSingleton<TodayMedicineVM>();
            builder.Services.AddSingleton<MedicineDetailVM>();
            builder.Services.AddSingleton<BaseInfoVM>();
            builder.Services.AddSingleton<NotificationInfoVM>();
            builder.Services.AddSingleton<StockInfoVM>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
