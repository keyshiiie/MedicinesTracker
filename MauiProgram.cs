using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Extensions.Configuration;
using MedicinesTracker.Repository;
using MedicinesTracker.ViewModels;
using MedicinesTracker.Modules.Medications.ViewModels;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Settings.ViewModels;
using MedicinesTracker.Services;
using MedicinesTracker.Modules.HistoryIntake.ViewModels;
using Plugin.LocalNotification;

namespace MedicinesTracker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if ANDROID
                .UseLocalNotification()
#endif 
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

            // 1. Регистрируем DatabaseService
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

            // 2. Регистрируем DBHandler с интерфейсом
            builder.Services.AddSingleton<IDBHandler, DBHandler>();

            // 3. Регистрируем репозитории (обновляем типы конструкторов в репозиториях!)
            builder.Services.AddSingleton<IMedicineRepository, MedicineRepository>();
            builder.Services.AddSingleton<IReferencesDataRepository, ReferencesDataRepository>();
            builder.Services.AddSingleton<IRecipientRepository, RecipientRepository>();
            builder.Services.AddSingleton<IStockRepository, StockRepository>();
            builder.Services.AddSingleton<IMedicineScheduleRepository, MedicineScheduleRepository>();
            builder.Services.AddSingleton<IIntakeRepository, IntakeRepository>();

            // 4. Регистрируем сервисы
            builder.Services.AddSingleton<IScheduleService, ScheduleService>();
            builder.Services.AddSingleton<IValidatorService, ValidatorService>();
            builder.Services.AddSingleton<IScheduleTimeRepository, ScheduleTimeRepository>();
            builder.Services.AddSingleton<IScheduleWeekDaysRepository, ScheduleWeekDaysRepository>();
            builder.Services.AddSingleton<IntakeSchedulerService>();
            builder.Services.AddSingleton<IPreferencesService, PreferencesService>();

            // 5. Регистрируем ViewModels
            builder.Services.AddSingleton<AppShellVM>();
            builder.Services.AddTransient<MedicineListVM>();
            builder.Services.AddTransient<TodayMedicineVM>();
            builder.Services.AddTransient<MedicineDetailVM>();
            builder.Services.AddTransient<BaseInfoVM>();
            builder.Services.AddTransient<MedicineScheduleVM>();
            builder.Services.AddTransient<StockInfoVM>();
            builder.Services.AddTransient<SettingsPageVM>();
            builder.Services.AddTransient<EditRecipientVM>();
            builder.Services.AddTransient<HistoryPageVM>();
            builder.Services.AddTransient<AcquaintanceVM>();
            builder.Services.AddTransient<GreetingVM>();    
            builder.Services.AddTransient<AboutAppVM>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}