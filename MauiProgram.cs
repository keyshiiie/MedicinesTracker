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
using MedicinesTracker.Data;
using Microsoft.EntityFrameworkCore;

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

            // Путь к базе данных (единый для всех платформ)
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MedicineTracker.db");

            // Регистрация DbContext как Scoped
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"),
                ServiceLifetime.Scoped);

            // Регистрация инициализатора БД
            builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            // Регистрация репозиториев
            builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
            builder.Services.AddScoped<IReferencesDataRepository, ReferencesDataRepository>();
            builder.Services.AddScoped<IRecipientRepository, RecipientRepository>();
            builder.Services.AddScoped<IStockRepository, StockRepository>();
            builder.Services.AddScoped<IMedicineScheduleRepository, MedicineScheduleRepository>();
            builder.Services.AddScoped<IIntakeRepository, IntakeRepository>();
            builder.Services.AddScoped<IScheduleTimeRepository, ScheduleTimeRepository>();
            builder.Services.AddScoped<IScheduleWeekDaysRepository, ScheduleWeekDaysRepository>();

            // Регистрация сервисов (ВСЕ Scoped)
            builder.Services.AddScoped<IScheduleService, ScheduleService>();
            builder.Services.AddScoped<IValidatorService, ValidatorService>();
            builder.Services.AddScoped<IIntakeGeneratorService, IntakeGeneratorService>();
            builder.Services.AddScoped<IPreferencesService, PreferencesService>();
            builder.Services.AddScoped<ITransactionHandler, TransactionHandler>();
            builder.Services.AddScoped<IMedicineBuilder, MedicineBuilder>();
            builder.Services.AddScoped<IScheduleEvaluator, ScheduleEvaluator>();
            builder.Services.AddScoped<INotificationPlannerService, NotificationPlannerService>();
            builder.Services.AddSingleton<StepManager>();
#if ANDROID
            builder.Services.AddSingleton<IAlarmScheduler>(sp =>
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as AlarmManager;
                return new MedicinesTracker.Platforms.Android.Services.AlarmScheduler(alarmManager, context);
            });
#else
            // Для Windows, iOS, MacCatalyst - временная заглушка
            builder.Services.AddSingleton<IAlarmScheduler, DummyAlarmScheduler>();
#endif

            // ViewModels (Transient)
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

            // Views (Transient)
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

            // AppShell как Singleton
            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Инициализация БД после сборки
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
                    dbInitializer.EnsureCreatedAsync().Wait();
                    System.Diagnostics.Debug.WriteLine("✅ Database initialized successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Database initialization failed: {ex.Message}");
                    throw;
                }
            }

            return app;
        }
    }
}