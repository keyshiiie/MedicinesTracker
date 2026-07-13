using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public partial class WeekDay : ObservableObject
    {
        [Key]
        public int IdDay { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }

        [ObservableProperty]
        private bool _isSelected;
    }
}