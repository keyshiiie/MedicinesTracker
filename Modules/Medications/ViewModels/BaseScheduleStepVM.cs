using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public abstract partial class BaseScheduleStepVM : ObservableObject
    {
        public abstract string Title { get; }
        public abstract string Description { get; }

        [ObservableProperty]
        private bool _isValid = false;

        [RelayCommand]
        public abstract Task ContinueAsync();

        [RelayCommand]
        public virtual Task BackAsync() => Task.CompletedTask;
    }
}