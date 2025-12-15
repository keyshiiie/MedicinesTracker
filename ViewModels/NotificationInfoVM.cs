using CommunityToolkit.Mvvm.ComponentModel;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(IdMedicine), "idMedicine")]
    public partial class NotificationInfoVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private int _idMedicine;

        [ObservableProperty]
        private IEnumerable<GroupedReminderDto> _groupedReminders = [];

        public NotificationInfoVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        public async Task InitializeAsync()
        {
            if (IdMedicine == 0) return;

            try
            {
                GroupedReminders = await _medicineService.GetAllRemindersByMedicineIdAsync(IdMedicine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }
    }

}
