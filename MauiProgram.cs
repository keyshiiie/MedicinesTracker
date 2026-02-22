using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Extensions.Configuration;
using MedicinesTracker.Repository;
using MedicinesTracker.Modules.Medications.ViewModels;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Settings.ViewModels;
using MedicinesTracker.Modules.HistoryIntake.ViewModels;
using MedicinesTracker.Services;
using Plugin.LocalNotification;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Modules.Medications.Views;
using MedicinesTracker.Modules.Settings.Views;
using MedicinesTracker.Modules.HistoryIntake.View;
#if ANDROID
using Android.App; 
using Android.Content;
#endif

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
            builder.Services.AddSingleton<IIntakeGeneratorService, IntakeGeneratorService>();
            builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
            builder.Services.AddScoped<ITransactionHandler, TransactionHandler>();
            builder.Services.AddScoped<IMedicineBuilder, MedicineBuilder>();
            builder.Services.AddSingleton<IScheduleEvaluator, ScheduleEvaluator>();

#if ANDROID
            builder.Services.AddSingleton<IAlarmScheduler>(sp =>
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as AlarmManager;
                return new MedicinesTracker.Platforms.Android.Services.AlarmScheduler(alarmManager, context);
            });
#endif
            builder.Services.AddSingleton<INotificationPlannerService, NotificationPlannerService>();

            // 5. Регистрируем ViewModels
            builder.Services.AddTransient<MedicineListVM>();
            builder.Services.AddTransient<TodayMedicineVM>();
            builder.Services.AddTransient<MedicineDetailVM>();
            builder.Services.AddTransient<BaseInfoVM>();
            builder.Services.AddTransient<ScheduleTypeSelectionVM>();
            builder.Services.AddTransient<ScheduleModeSelectionVM>();
            builder.Services.AddTransient<ScheduleDetailsVM>();
            builder.Services.AddTransient<StockInfoVM>();
            builder.Services.AddTransient<SettingsPageVM>();
            builder.Services.AddTransient<EditRecipientVM>();
            builder.Services.AddTransient<HistoryPageVM>();
            builder.Services.AddTransient<AcquaintanceVM>();
            builder.Services.AddTransient<GreetingVM>();
            builder.Services.AddTransient<AboutAppVM>();

            // 6. Регистрируем ВСЕ Views (включая те, что в TabBar)
            builder.Services.AddTransient<MedicineListPage>();
            builder.Services.AddTransient<TodayMedicinePage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<BaseInfoPage>();
            builder.Services.AddTransient<MedicineDetailPage>();
            builder.Services.AddTransient<ScheduleTypeSelectionPage>();
            builder.Services.AddTransient<ScheduleModeSelectionPage>();
            builder.Services.AddTransient<ScheduleDetailsPage>();
            builder.Services.AddTransient<StockInfoPage>();
            builder.Services.AddTransient<EditRecipientPage>();
            builder.Services.AddTransient<GreetingPage>();
            builder.Services.AddTransient<AcquaintancePage>();
            builder.Services.AddTransient<AboutAppPage>();

            // 7. Регистрируем AppShell как Singleton
            builder.Services.AddSingleton<AppShell>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}