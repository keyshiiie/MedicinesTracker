using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Extensions.Configuration;
using MedicinesTracker.Repository;
using MedicinesTracker.ViewModels;
using MedicinesTracker.Modules.Medications.ViewModels;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Settings.ViewModels;

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
            builder.Services.AddSingleton<IReferencesDataRepository, ReferencesDataRepository>();
            builder.Services.AddSingleton<IRecipientRepository, RecipientRepository>();
            builder.Services.AddSingleton<IStockRepository, StockRepository>();
            builder.Services.AddSingleton<IMedicineScheduleRepository, MedicineScheduleRepository>();

            
            builder.Services.AddSingleton<IMedicineRepository, MedicineRepository>();

            builder.Services.AddSingleton<AppShellVM>();
            builder.Services.AddSingleton<MedicineListVM>();
            builder.Services.AddSingleton<TodayMedicineVM>();
            builder.Services.AddSingleton<MedicineDetailVM>();
            builder.Services.AddSingleton<BaseInfoVM>();
            builder.Services.AddSingleton<MedicineScheduleVM>();
            builder.Services.AddSingleton<StockInfoVM>();
            builder.Services.AddSingleton<SettingsPageVM>();
            builder.Services.AddSingleton<EditRecipientVM>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
