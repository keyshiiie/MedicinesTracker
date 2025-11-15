using MedicinesTracker.ViewModels;
using MedicinesTracker.Views;
using System.Diagnostics;

namespace MedicinesTracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            BindingContext = new AppShellVM();
            Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
            Routing.RegisterRoute("MedicineDetailPage", typeof(MedicineDetailPage));
            Routing.RegisterRoute("BaseInfoPage", typeof(BaseInfoPage));
            Routing.RegisterRoute("StockInfoPage", typeof(StockInfoPage));
            Routing.RegisterRoute("NotificationInfoPage", typeof(NotificationInfoPage));
        }
    }
}
