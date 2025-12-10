using MedicinesTracker.ViewModels;

namespace MedicinesTracker
{
    public partial class App : Application
    {
        private readonly AppShellVM _appShellVM;
        public App()
        {
            InitializeComponent();
            _appShellVM = new AppShellVM();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(_appShellVM));
        }
    }
}