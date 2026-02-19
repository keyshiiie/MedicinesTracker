using MedicinesTracker.Modules.Medications.Views;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Modules.Settings.Views;
using MedicinesTracker.Views;

namespace MedicinesTracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            // Существующие маршруты
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("MedicineDetailPage", typeof(MedicineDetailPage));
            Routing.RegisterRoute("BaseInfoPage", typeof(BaseInfoPage));
            Routing.RegisterRoute("StockInfoPage", typeof(StockInfoPage));
            Routing.RegisterRoute("EditRecipientPage", typeof(EditRecipientPage));
            Routing.RegisterRoute("GreetingPage", typeof(GreetingPage));
            Routing.RegisterRoute("AcquaintancePage", typeof(AcquaintancePage));
            Routing.RegisterRoute("AboutAppPage", typeof(AboutAppPage));

            // Новые маршруты для многошагового создания расписания
            Routing.RegisterRoute("ScheduleTypeSelectionPage", typeof(ScheduleTypeSelectionPage));
            Routing.RegisterRoute("ScheduleModeSelectionPage", typeof(ScheduleModeSelectionPage));
            Routing.RegisterRoute("ScheduleDetailsPage", typeof(ScheduleDetailsPage));
        }

        private async void SettingsToolbarItem_Clicked(object sender, EventArgs e)
        {
            await NavigateToSettingsAsync();
        }

        private async Task NavigateToSettingsAsync()
        {
            // Проверяем, не открыта ли уже страница настроек
            var currentPage = Shell.Current.CurrentPage;

            if (currentPage?.GetType() != typeof(SettingsPage))
            {
                // Открываем модально, чтобы не плодить страницы в стеке
                await Shell.Current.GoToAsync("SettingsPage", true);
            }
        }
    }
}