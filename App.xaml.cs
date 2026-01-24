using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Services;
using MedicinesTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MedicinesTracker
{
    public partial class App : Application
    {
        private readonly AppShellVM _appShellVM;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPreferencesService _preferencesService;
        private readonly IIntakeSchedulerService _intakeScheduler;
        private NavigationPage? _introNavigationPage;

        public App(
            AppShellVM appShellVM,
            IServiceProvider serviceProvider,
            IPreferencesService preferencesService,
            IIntakeSchedulerService intakeScheduler) 
        {
            InitializeComponent();
            _appShellVM = appShellVM;
            _serviceProvider = serviceProvider;
            _preferencesService = preferencesService;
            _intakeScheduler = intakeScheduler; 
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
                    // Просто заменяем текущую страницу на AppShell
                    var appShell = new AppShell(_appShellVM);

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
            // Запускаем запрос разрешений асинхронно
            Task.Run(async () =>
            {
                await RequestNotificationPermissionIfNeeded();
            });

            return new Window(new AppShell(_appShellVM));
        }

        protected override async void OnStart()
        {
            base.OnStart();

            try
            {
                Debug.WriteLine("=== App.OnStart ===");

                // 1. СРАЗУ запрашиваем разрешение на уведомления
                await RequestNotificationPermissionIfNeeded();

                // 2. Генерируем записи только на сегодня
                await _intakeScheduler.GenerateTodayIntakesAsync();

                // 3. Инициализируем уведомления только на сегодня
                var notificationService = _serviceProvider.GetRequiredService<INotificationSchedulerService>();
                await notificationService.ScheduleNotificationsForTodayAsync();

                // 4. Запускаем ежедневную проверку в 00:01
                StartDailyCheck();

                Debug.WriteLine("=== App.OnStart завершен ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при запуске: {ex.Message}");
            }
        }

        private async Task RequestNotificationPermissionIfNeeded()
        {
            try
            {
                Debug.WriteLine("=== Запрос разрешений на уведомления ===");

                var permissionRequested = Preferences.Get("NotificationPermissionRequested", false);

                if (!permissionRequested)
                {
                    Debug.WriteLine("Запрашиваем разрешение впервые...");
                    bool hasPermission = await NotificationPermissionService.CheckAndRequestAsync();
                    Preferences.Set("NotificationPermissionRequested", true);
                    Debug.WriteLine($"Разрешение на уведомления: {hasPermission}");

                    // ЗАПУСКАЕМ СЛУЖБУ ТОЛЬКО ПОСЛЕ ПОЛУЧЕНИЯ РАЗРЕШЕНИЙ
                    if (hasPermission)
                    {
                        StartBackgroundService();
                    }
                }
                else
                {
                    Debug.WriteLine("Разрешение уже было запрошено ранее");
                    // Если разрешение уже было, запускаем службу сразу
                    StartBackgroundService();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при запросе разрешения: {ex.Message}");
            }
        }

        private void StartBackgroundService()
        {
#if ANDROID
            try
            {
                Debug.WriteLine("🚀 Запускаем фоновую службу уведомлений...");

                // Запускаем службу
                ForegroundServiceStarter.StartForegroundService();
                Debug.WriteLine("✅ Фоновая служба запущена");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка запуска фоновой службы: {ex.Message}");
            }
#endif
        }

        private void StartDailyCheck()
        {
            // Запускаем таймер на ежедневную генерацию в 00:01
            Dispatcher.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                var now = DateTime.Now;
                if (now.Hour == 0 && now.Minute == 1)
                {
                    Debug.WriteLine("🔄 Ежедневная генерация записей на сегодня в 00:01");

                    Task.Run(async () =>
                    {
                        try
                        {
                            // Генерируем записи на сегодня
                            await _intakeScheduler.GenerateTodayIntakesAsync();

                            // Планируем уведомления на сегодня
                            var notificationService = _serviceProvider.GetRequiredService<INotificationSchedulerService>();
                            await notificationService.ScheduleNotificationsForTodayAsync();

                            Debug.WriteLine("✅ Ежедневная генерация завершена");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"❌ Ошибка ежедневной генерации: {ex.Message}");
                        }
                    });
                }
                return true;
            });
        }

        protected override async void OnResume()
        {
            base.OnResume();
            Debug.WriteLine("=== App.OnResume ===");

            try
            {
                // 1. Проверяем, не сменился ли день
                await CheckDayChangeAndGenerateIntakes();

                // 2. Перепланируем уведомления на сегодня
                await RescheduleNotificationsIfNeeded();

                // 3. Перезапускаем фоновую службу (на случай если она остановилась)
#if ANDROID
                StartBackgroundService();
#endif

                Debug.WriteLine("✅ Приложение возобновлено и обновлено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при возобновлении: {ex.Message}");
            }
        }

        private async Task CheckDayChangeAndGenerateIntakes()
        {
            try
            {
                // Получаем время, когда приложение ушло в сон
                var lastSleepStr = Preferences.Get("LastAppSleepTime", string.Empty);

                if (!string.IsNullOrEmpty(lastSleepStr) &&
                    DateTime.TryParse(lastSleepStr, out var lastSleepTime))
                {
                    // Проверяем, сменился ли день
                    var now = DateTime.Now;

                    if (lastSleepTime.Date < now.Date)
                    {
                        Debug.WriteLine("📅 Обнаружена смена дня, генерируем новые записи на сегодня...");
                        await _intakeScheduler.GenerateTodayIntakesAsync();
                    }
                    else
                    {
                        Debug.WriteLine("✅ Тот же день, проверяем наличие записей на сегодня...");
                        // Все равно проверяем, есть ли записи на сегодня
                        await _intakeScheduler.CheckAndUpdateAsync();
                    }
                }
                else
                {
                    // Если нет сохраненного времени, просто проверяем
                    Debug.WriteLine("🔄 Проверяем наличие записей на сегодня...");
                    await _intakeScheduler.GenerateTodayIntakesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке смены дня: {ex.Message}");
            }
        }

        private async Task RescheduleNotificationsIfNeeded()
        {
            try
            {
                var notificationService = _serviceProvider.GetRequiredService<INotificationSchedulerService>();

                // Отменяем старые уведомления и планируем новые на сегодня
                notificationService.CancelAllNotifications();
                await notificationService.ScheduleNotificationsForTodayAsync();

                Debug.WriteLine("✅ Уведомления перепланированы на сегодня");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при перепланировании уведомлений: {ex.Message}");
            }
        }

        private void StartIntakeGenerationInBackground()
        {
            // Запускаем в фоне с задержкой, чтобы не блокировать запуск приложения
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000); // Даем UI полностью загрузиться

                    Debug.WriteLine("🚀 Запуск генерации записей приема лекарств...");

                    // Генерируем записи на сегодня (если еще не генерировали)
                    await _intakeScheduler.GenerateTodayIntakesAsync();

                    Debug.WriteLine("✅ Генерация записей приема завершена");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка при генерации записей: {ex.Message}");
                }
            });
        }

        private void ScheduleNotificationsInBackground()
        {
            // Уведомления планируем в фоне
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000); // Еще небольшая задержка

                    var notificationService = _serviceProvider.GetRequiredService<INotificationSchedulerService>();

                    Debug.WriteLine("📅 Планируем уведомления...");
                    await notificationService.ScheduleAllNotificationsAsync();

                    Debug.WriteLine("✅ Уведомления запланированы");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка при планировании уведомлений: {ex.Message}");
                }
            });
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            Debug.WriteLine("=== App.OnSleep ===");

            // Сохраняем время, когда приложение ушло в сон
            Preferences.Set("LastAppSleepTime", DateTime.Now.ToString("O"));

#if ANDROID
            // Фоновая служба продолжит работать, но можем добавить логику при необходимости
            Debug.WriteLine("Фоновая служба продолжит работу");
#endif
        }

        

        // Убираем старый метод StartBackgroundCheck(), т.к. теперь генерация по событиям
        // Но оставляем для совместимости или уведомлений если нужно
        private void StartPeriodicBackgroundCheck()
        {
            if (Dispatcher is null) return;

            Debug.WriteLine("Запускаем периодическую проверку каждые 12 часов...");

            Dispatcher.StartTimer(TimeSpan.FromHours(12), () =>
            {
                Debug.WriteLine("=== Периодическая проверка ===");

                Task.Run(async () =>
                {
                    try
                    {
                        // Проверяем только уведомления
                        var notificationService = _serviceProvider.GetRequiredService<INotificationSchedulerService>();
                        await notificationService.ScheduleAllNotificationsAsync();

                        Debug.WriteLine("✅ Периодическая проверка уведомлений завершена");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Ошибка периодической проверки: {ex.Message}");
                    }
                });

                return true;
            });
        }
    }
}