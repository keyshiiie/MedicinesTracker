using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;

namespace MedicinesTracker.ViewModels
{
    public class TodayMedicineVM : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private ObservableCollection<ReminderModel> _reminders = new ObservableCollection<ReminderModel>();

        public ObservableCollection<ReminderModel> Reminders
        {
            get => _reminders;
            private set
            {
                if (_reminders != value)
                {
                    _reminders = value;
                    OnPropertyChanged(nameof(Reminders));
                }
            }
        }

        public TodayMedicineVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
            LoadRemindersList();
        }

        private void LoadRemindersList()
        {
            try
            {/*
                var reminders = _medicineService.GetTodayMedicines();
                foreach (var reminder in reminders)
                {
                    Reminders.Add(reminder);
                }*/
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка загрузки данных.", ex);
            }
        }
    }
}
