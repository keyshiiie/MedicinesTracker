using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace MedicinesTracker.Modules.Notifications.ViewModels
{
    public partial class GreetingVM : ObservableObject
    {
        [RelayCommand]
        private void ContinueToAcquaintance()
        {
            // Используем Messaging Center для уведомления о необходимости навигации
            WeakReferenceMessenger.Default.Send(new NavigationMessage("AcquaintancePage"));
        }
    }

    // Сообщение для навигации
    public class NavigationMessage
    {
        public string Route { get; }

        public NavigationMessage(string route)
        {
            Route = route;
        }
    }
}