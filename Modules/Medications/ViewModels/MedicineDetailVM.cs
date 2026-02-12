using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(ScheduleId), "idSchedule")]
    [QueryProperty(nameof(StockId), "idStock")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class MedicineDetailVM : ObservableObject
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IScheduleService _scheduleService;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private int _scheduleId = 0;

        [ObservableProperty]
        private int _stockId = 0;

        [ObservableProperty]
        private string? _unitName;

        public MedicineDetailVM(IMedicineRepository medicineRepository, IScheduleService scheduleService)
        {
            _medicineRepository = medicineRepository;
            _scheduleService = scheduleService;
        }

        [RelayCommand]
        private async Task OpenBaseInfoPage()
        {
            if (MedicineId <= 0) return;
            try
            {
                var route = "BaseInfoPage";
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", MedicineId }
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу редактирования: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenNotificationPage()
        {
            if (MedicineId <= 0) return;

            try
            {
                if (ScheduleId > 0)
                {
                    // Редактирование существующего расписания
                    // Сначала загрузим расписание, чтобы узнать его тип и режим
                    var schedule = await _scheduleService.GetScheduleByIdAsync(ScheduleId);
                    if (schedule != null)
                    {
                        var parameters = new Dictionary<string, object>
                        {
                            { "scheduleId", ScheduleId },
                            { "medicineId", MedicineId },
                            { "isNewMedicine", false },
                            { "scheduleTypeCode", GetScheduleTypeCode(schedule.IdScheduleType) },
                            { "scheduleModeCode", GetScheduleModeCode(schedule.IdScheduleMode) }
                        };
                        await Shell.Current.GoToAsync("ScheduleDetailsPage", parameters);
                    }
                    else
                    {
                        // Расписание не найдено, создаем новое
                        await CreateNewSchedule();
                    }
                }
                else
                {
                    // Создание нового расписания
                    await CreateNewSchedule();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу расписания: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось открыть расписание", "OK");
            }
        }

        private async Task CreateNewSchedule()
        {
            // Создание нового расписания - переходим к выбору типа
            var parameters = new Dictionary<string, object>
            {
                { "medicineId", MedicineId },
                { "isNewMedicine", false }
            };
            await Shell.Current.GoToAsync("ScheduleTypeSelectionPage", parameters);
        }

        private string GetScheduleTypeCode(int? idScheduleType)
        {
            return idScheduleType switch
            {
                1 => "RECURRING",
                2 => "ONETIME",
                _ => "RECURRING" // по умолчанию
            };
        }

        private string? GetScheduleModeCode(int? idScheduleMode)
        {
            return idScheduleMode switch
            {
                1 => "INTERVAL",
                2 => "WEEKDAYS",
                _ => null
            };
        }

        [RelayCommand]
        private async Task OpenStockPage()
        {
            try
            {
                var route = "StockInfoPage";
                var parameters = new Dictionary<string, object>
                {
                    { "idStock", StockId},
                    { "unitName", UnitName ?? string.Empty},
                    { "idMedicine", MedicineId}
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу запасов: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteMedicine()
        {
            try
            {
                if (MedicineId <= 0) return;

                bool confirmDelete = await Shell.Current.DisplayAlertAsync(
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить это лекарство?",
                    "Да",
                    "Нет"
                );

                if (!confirmDelete) return;

                var rowsAffected = await _medicineRepository.DeleteMedicineAsync(MedicineId);
                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        "Лекарство успешно удалёно!",
                        "ОК");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Лекарство не было удалёно",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка!",
                    $"Не удалось удалить: {ex.Message}",
                    "ОК");
            }
        }
    }
}