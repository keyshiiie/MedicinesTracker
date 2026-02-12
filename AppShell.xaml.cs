// AppShell.xaml.cs
using MedicinesTracker.Modules.Medications.Views;
using MedicinesTracker.Modules.Notifications.Views;
using MedicinesTracker.Modules.Settings.Views;
using MedicinesTracker.ViewModels;
using MedicinesTracker.Views;

namespace MedicinesTracker
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellVM viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Существующие маршруты
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("MedicineDetailPage", typeof(MedicineDetailPage));
            Routing.RegisterRoute("BaseInfoPage", typeof(BaseInfoPage));
            // УДАЛИТЬ: Routing.RegisterRoute("MedicineSchedulePage", typeof(MedicineSchedulePage));
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
    }
}