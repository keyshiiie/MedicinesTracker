using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
