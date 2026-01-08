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
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("MedicineDetailPage", typeof(MedicineDetailPage));
            Routing.RegisterRoute("BaseInfoPage", typeof(BaseInfoPage));
            Routing.RegisterRoute("MedicineSchedulePage", typeof(MedicineSchedulePage));
            Routing.RegisterRoute("StockInfoPage", typeof(StockInfoPage));
            Routing.RegisterRoute("EditRecipientPage", typeof(EditRecipientPage));
            Routing.RegisterRoute("GreetingPage", typeof(GreetingPage));
            Routing.RegisterRoute("AcquaintancePage", typeof(AcquaintancePage));
            Routing.RegisterRoute("AboutAppPage", typeof(AboutAppPage));
        }
    }
}
