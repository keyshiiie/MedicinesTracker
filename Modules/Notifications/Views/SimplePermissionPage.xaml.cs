using MedicinesTracker.Services;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class SimplePermissionPage : ContentPage
    {
        public SimplePermissionPage()
        {
            InitializeComponent();
        }

        private async void OnAllowClicked(object sender, EventArgs e)
        {
            var granted = await NotificationPermissionService.CheckAndRequestAsync();

            if (!granted)
            {
                // Показываем алерт с предложением открыть настройки
                bool openSettings = await DisplayAlertAsync(
                    "Нужно разрешение",
                    "Откройте настройки и разрешите уведомления",
                    "Настройки",
                    "Позже");

                if (openSettings)
                {
                    NotificationPermissionService.OpenAppSettings();
                }
            }

            await Navigation.PopModalAsync();
        }

        private async void OnLaterClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}