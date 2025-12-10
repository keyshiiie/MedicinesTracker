using MedicinesTracker.ViewModels;
using MedicinesTracker.Views;
using System.Diagnostics;

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
            Routing.RegisterRoute("NotificationInfoPage", typeof(NotificationInfoPage));
            Routing.RegisterRoute("StockInfoPage", typeof(StockInfoPage));
        }
    }
}
