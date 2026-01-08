using System.Windows.Input;

namespace MedicinesTracker.ViewModels
{
    public class AppShellVM
    {
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand NavigateToGreetingCommand { get; } // Для тестирования

        public AppShellVM()
        {
            NavigateToSettingsCommand = new Command(NavigateToSettings);
            NavigateToGreetingCommand = new Command(NavigateToGreeting);
        }

        private async void NavigateToSettings()
        {
            await Shell.Current.GoToAsync("SettingsPage");
        }

        private async void NavigateToGreeting()
        {
            await Shell.Current.GoToAsync("GreetingPage");
        }
    }
}