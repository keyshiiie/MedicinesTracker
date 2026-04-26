using CommunityToolkit.Mvvm.ComponentModel;

namespace MedicinesTracker.Modules.Medications.Models
{
    public class ButtonUiState : ObservableObject
    {
        private string _text = "Далее";
        private bool _isPrimary = true;
        private bool _isEnabled = true;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public bool IsPrimary
        {
            get => _isPrimary;
            set => SetProperty(ref _isPrimary, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }
}