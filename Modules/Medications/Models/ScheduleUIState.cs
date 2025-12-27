using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Modules.Medications.Models
{
    public partial class ScheduleUIState : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowFrequencyPicker))]
        [NotifyPropertyChangedFor(nameof(ShowWeekDaysPicker))]
        [NotifyPropertyChangedFor(nameof(ShowRecurringDateFields))]
        [NotifyPropertyChangedFor(nameof(ShowOneTimeDateField))]
        private bool _isRecurringSchedule; // true, если RECURRING; false, если ONETIME

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowFrequencyPicker))]
        [NotifyPropertyChangedFor(nameof(ShowWeekDaysPicker))]
        private bool _isIntervalMode; // true, если INTERVAL; false, если WEEKDAYS

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowFrequencyPicker))]
        [NotifyPropertyChangedFor(nameof(ShowWeekDaysPicker))]
        private bool _isWeekDaysMode; // true, если WEEKDAYS; false, если INTERVAL

        // Вычисляемые свойства для видимсоти полей в UI
        public bool ShowFrequencyPicker => IsRecurringSchedule && IsIntervalMode;
        public bool ShowWeekDaysPicker => IsRecurringSchedule && IsWeekDaysMode;
        public bool ShowRecurringDateFields => IsRecurringSchedule;
        public bool ShowOneTimeDateField => !IsRecurringSchedule;

        public void UpdateForScheduleType(string? typeCode)
        {
            IsRecurringSchedule = typeCode == "RECURRING";
        }
        public void UpdateForScheduleMode(string? modeCode)
        {
            IsIntervalMode = modeCode == "INTERVAL";
            IsWeekDaysMode = modeCode == "WEEKDAYS";
        }
        public void Reset()
        {
            IsRecurringSchedule = false;
            IsIntervalMode = false;
            IsWeekDaysMode = false;
        }
    }
}
