#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif
using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;  // Для System.Diagnostics.Debug
using Application = Microsoft.Maui.Controls.Application;  // Алиас для MAUI Application

namespace MedicinesTracker
{
    public partial class App : Application  // Теперь это MAUI Application
    {
        // Удаляем _appShellVM
        private readonly IServiceProvider _serviceProvider;
        private readonly IPreferencesService _preferencesService;
        private readonly IIntakeGeneratorService _intakeGenerator;
        private NavigationPage? _introNavigationPage;

        public App(
            // Удаляем AppShellVM из параметров
            IServiceProvider serviceProvider,
            IPreferencesService preferencesService,
            IIntakeGeneratorService intakeGenerator)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _preferencesService = preferencesService;
            _intakeGenerator = intakeGenerator;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Проверяем, был ли выполнен первый запуск
            bool firstLaunchCompleted = _preferencesService.Get("FirstLaunchCompleted", false);

            if (!firstLaunchCompleted)
            {
                return CreateIntroWindow();
            }
            else
            {
                return CreateMainWindow();
            }
        }

        private Window CreateIntroWindow()
        {
            var greetingVM = _serviceProvider.GetRequiredService<GreetingVM>();
            var greetingPage = new GreetingPage(greetingVM);

            _introNavigationPage = new NavigationPage(greetingPage)
            {
                BarBackgroundColor = Colors.Transparent,
                BarTextColor = Colors.Transparent
            };

            NavigationPage.SetHasNavigationBar(greetingPage, false);

            // Подписываемся на сообщения о навигации
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (m.Route)
                    {
                        case "AcquaintancePage":
                            var acquaintanceVM = _serviceProvider.GetRequiredService<AcquaintanceVM>();
                            await _introNavigationPage.Navigation.PushAsync(
                                new AcquaintancePage(acquaintanceVM));
                            break;

                        case "AboutAppPage":
                            var aboutAppVM = _serviceProvider.GetRequiredService<AboutAppVM>();
                            await _introNavigationPage.Navigation.PushAsync(
                                new AboutAppPage(aboutAppVM));
                            break;
                    }
                });
            });

            // Подписываемся на сообщение о переходе к AppShell
            WeakReferenceMessenger.Default.Register<AppShellNavigationMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Создаем AppShell без ViewModel
                    var appShell = new AppShell(); // Убрали _appShellVM

                    // Получаем текущее окно
                    var window = Application.Current?.Windows.FirstOrDefault();
                    if (window != null)
                    {
                        window.Page = appShell;
                    }
                });
            });

            return new Window(_introNavigationPage);
        }

        private Window CreateMainWindow()
        {
            // Создаем AppShell без ViewModel
            var appShell = new AppShell(); // Убрали _appShellVM

            // Запускаем запрос разрешений асинхронно
            Task.Run(async () =>
            {
                await RequestNotificationPermissionIfNeeded();
            });

            return new Window(appShell);
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                System.Diagnostics.Debug.WriteLine("=== App.OnStart ===");

                // 1. Запрашиваем разрешения на уведомления
                await RequestNotificationPermissionIfNeeded();

                // 2. Запрашиваем разрешение на точные будильники (Android 12+)
                await RequestExactAlarmPermissionIfNeeded();

                // 3. Генерируем записи на сегодня
                await _intakeGenerator.GenerateTodayIntakesAsync();

                // 4. Планируем уведомления
                var notificationPlanner = _serviceProvider.GetRequiredService<INotificationPlannerService>();
                await notificationPlanner.PlanForTodayAsync();

                // 5. Запускаем ежедневную проверку
                StartDailyCheck();

                System.Diagnostics.Debug.WriteLine("✅ App.OnStart завершен");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task RequestExactAlarmPermissionIfNeeded()
        {
            try
            {
#if ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    var context = Android.App.Application.Context;
                    var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;

                    if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Нет разрешения на точные будильники");

                        // Проверяем, запрашивали ли уже
                        bool asked = Preferences.Get("ExactAlarmPermissionAsked", false);

                        if (!asked)
                        {
                            var confirm = await Application.Current.MainPage.DisplayAlertAsync(
                                "Разрешение на точные уведомления",
                                "Для своевременных напоминаний приложению нужно разрешение на точные будильники.\n\nХотите предоставить его?",
                                "Да",
                                "Не сейчас");

                            if (confirm)
                            {
                                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                                intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                                intent.SetFlags(ActivityFlags.NewTask);
                                context.StartActivity(intent);
                            }

                            Preferences.Set("ExactAlarmPermissionAsked", true);
                        }
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запросе разрешения: {ex.Message}");
            }
        }

        private async Task RequestNotificationPermissionIfNeeded()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Запрос разрешений на уведомления ===");

                var permissionRequested = Preferences.Get("NotificationPermissionRequested", false);

                if (!permissionRequested)
                {
                    System.Diagnostics.Debug.WriteLine("Запрашиваем разрешение впервые...");
                    bool hasPermission = await NotificationPermissionService.CheckAndRequestAsync();
                    Preferences.Set("NotificationPermissionRequested", true);
                    System.Diagnostics.Debug.WriteLine($"Разрешение на уведомления: {hasPermission}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Разрешение уже было запрошено ранее");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запросе разрешения: {ex.Message}");
            }
        }

        private void StartDailyCheck()
        {
            Dispatcher.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                var now = DateTime.Now;
                if (now.Hour == 0 && now.Minute == 1)
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Ежедневная генерация записей на сегодня в 00:01");

                    Task.Run(async () =>
                    {
                        try
                        {
                            await _intakeGenerator.GenerateTodayIntakesAsync();

                            var notificationPlanner = _serviceProvider.GetRequiredService<INotificationPlannerService>();
                            await notificationPlanner.PlanForTodayAsync();

                            System.Diagnostics.Debug.WriteLine("✅ Ежедневная генерация завершена");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Ошибка ежедневной генерации: {ex.Message}");
                        }
                    });
                }
                return true;
            });
        }

        protected override async void OnResume()
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine("=== App.OnResume ===");

            try
            {
                await CheckDayChangeAndGenerateIntakes();
                await RescheduleNotificationsIfNeeded();
                System.Diagnostics.Debug.WriteLine("✅ Приложение возобновлено и обновлено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при возобновлении: {ex.Message}");
            }
        }

        private async Task CheckDayChangeAndGenerateIntakes()
        {
            try
            {
                var lastSleepStr = Preferences.Get("LastAppSleepTime", string.Empty);

                if (!string.IsNullOrEmpty(lastSleepStr) &&
                    DateTime.TryParse(lastSleepStr, out var lastSleepTime))
                {
                    var now = DateTime.Now;

                    if (lastSleepTime.Date < now.Date)
                    {
                        System.Diagnostics.Debug.WriteLine("📅 Обнаружена смена дня, генерируем новые записи на сегодня...");
                        await _intakeGenerator.GenerateTodayIntakesAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Тот же день, проверяем наличие записей на сегодня...");
                        await _intakeGenerator.CheckAndUpdateAsync();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Проверяем наличие записей на сегодня...");
                    await _intakeGenerator.GenerateTodayIntakesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке смены дня: {ex.Message}");
            }
        }

        private async Task RescheduleNotificationsIfNeeded()
        {
            try
            {
                var notificationPlanner = _serviceProvider.GetRequiredService<INotificationPlannerService>();

                notificationPlanner.CancelAll();
                await notificationPlanner.PlanForTodayAsync();

                System.Diagnostics.Debug.WriteLine("✅ Уведомления перепланированы на сегодня");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при перепланировании уведомлений: {ex.Message}");
            }
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            System.Diagnostics.Debug.WriteLine("=== App.OnSleep ===");
            Preferences.Set("LastAppSleepTime", DateTime.Now.ToString("O"));
        }
    }
}