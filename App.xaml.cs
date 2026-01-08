using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Services;
using MedicinesTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MedicinesTracker
{
    public partial class App : Application
    {
        private readonly AppShellVM _appShellVM;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPreferencesService _preferencesService;
        private IntakeSchedulerService? _schedulerService;
        private NavigationPage? _introNavigationPage;

        public App(AppShellVM appShellVM, IServiceProvider serviceProvider,
                   IPreferencesService preferencesService)
        {
            InitializeComponent();
            _appShellVM = appShellVM;
            _serviceProvider = serviceProvider;
            _preferencesService = preferencesService;
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
            return new Window(new AppShell(_appShellVM));
        }

        protected override async void OnStart()
        {
            base.OnStart();

            _schedulerService = _serviceProvider.GetRequiredService<IntakeSchedulerService>();
            await _schedulerService.InitializeAsync();
            StartBackgroundCheck();
        }

        protected override async void OnResume()
        {
            base.OnResume();

            // Проверяем при возвращении в приложение
            if (_schedulerService != null)
            {
                await _schedulerService.CheckAndUpdateAsync();
            }
        }

        private void StartBackgroundCheck()
        {
            if (Dispatcher is null) return;

            // Используем Dispatcher вместо устаревшего Device.StartTimer
            Dispatcher.StartTimer(TimeSpan.FromHours(6), () =>
            {
                Task.Run(async () =>
                {
                    if (_schedulerService != null)
                    {
                        await _schedulerService.CheckAndUpdateAsync();
                    }
                });
                return true; // Продолжаем таймер
            });
        }
    }
}