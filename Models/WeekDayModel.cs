using CommunityToolkit.Mvvm.ComponentModel;

namespace MedicinesTracker.Models
{
    public partial class WeekDayModel : ObservableObject
    {
        public int IdDay { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }

        [ObservableProperty]
        private bool _isSelected;
    }
}