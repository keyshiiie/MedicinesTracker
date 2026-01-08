using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace MedicinesTracker.Modules.Notifications.ViewModels
{
    public partial class AboutAppVM : ObservableObject
    {
        [RelayCommand]
        private void ContinueToMainApp()
        {
            // Отправляем сообщение для перехода к главному приложению
            WeakReferenceMessenger.Default.Send(new AppShellNavigationMessage());
        }
    }

    // Отдельное сообщение для перехода к AppShell
    public class AppShellNavigationMessage
    {
    }
}