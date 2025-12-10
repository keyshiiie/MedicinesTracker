using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    public partial class MedicineListVM : ObservableObject
    {
        private readonly MedicineService _medicineService;
        [ObservableProperty]
        private IEnumerable<MedicineDetailDto> _medicineDetails = [];


        public MedicineListVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("[MedicineListVM] InitializeAsync начат");
                await LoadDataAsync();
                Debug.WriteLine("[MedicineListVM] InitializeAsync завершен");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MedicineListVM ERROR] {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenDetailPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;
            try
            {
                var route = "MedicineDetailPage";
                var parameters = new Dictionary<string, object>
                {
                    { "medicine", medicine } 
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу детализации: {ex.Message}");
            }
        }


        private async Task LoadDataAsync()
        {
            try
            {
                var rawData = await _medicineService.GetAllMedicineDetailsAsync();

                // Группируем по IdMedicine, оставляем только первое напоминание для карточки
                var grouped = rawData
                    .GroupBy(d => d.IdMedicine)
                    .Select(group =>
                    {
                        // Берём первую запись группы (для отображения в карточке)
                        var firstItem = group.First();

                        // Формируем ScheduleString для всех напоминаний лекарства
                        var schedule = FormatSchedule(group);

                        // Создаём новую DTO с общим расписанием
                        return new MedicineDetailDto
                        {
                            IdMedicine = firstItem.IdMedicine,
                            MedicineName = firstItem.MedicineName,
                            MethodAdmissionName = firstItem.MethodAdmissionName,
                            UnitName = firstItem.UnitName,
                            CurrentQuantity = firstItem.CurrentQuantity,
                            ReminderEnabled = firstItem.ReminderEnabled,
                            RecipientName = firstItem.RecipientName,
                            ScheduleString = schedule
                        };
                    })
                    .ToList();

                MedicineDetails = grouped;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }


        private string FormatSchedule(IEnumerable<MedicineDetailDto> reminders)
        {
            var times = reminders
                .Select(r => r.Time)
                .OrderBy(t => t)
                .ToList();

            if (!times.Any())
                return string.Empty;

            var frequencyText = $"{times.Count} раз(а) в день";

            if (times.Count == 1)
                return $"{frequencyText} — {times[0]}";
            else if (times.Count == 2)
                return $"{frequencyText} — {times[0]} и {times[1]}";
            else
                return $"{frequencyText} — {string.Join(", ", times.Take(times.Count - 1))} и {times.Last()}";
        }

    }
}
