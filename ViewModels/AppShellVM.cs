using System.Windows.Input;

namespace MedicinesTracker.ViewModels
{
    public class AppShellVM
    {
        public ICommand NavigateToSettingsCommand { get; }

        public AppShellVM()
        {
            NavigateToSettingsCommand = new Command(NavigateToSettings);
        }

        private async void NavigateToSettings()
        {
            await Shell.Current.GoToAsync("SettingsPage");
        }
    }
}
