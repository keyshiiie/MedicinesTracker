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

        [ObservableProperty]
        private int _medicineId; // 0 = добавление, > 0 = редактирование

        [ObservableProperty]
        private int _scheduleId = 0; // 0 = добавление, > 0 = редактирование

        [ObservableProperty]
        private int _stockId = 0;

        [ObservableProperty]
        private string? _unitName;

        public MedicineDetailVM(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
        }

        [RelayCommand]
        private async Task OpenBaseInfoPage()
        {
            if (MedicineId <= 0) return; // 0 или -1
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
            if (MedicineId <= 0) return; // 0 или -1

            var route = "MedicineSchedulePage";
            var parameters = new Dictionary<string, object>
            {
                { "idSchedule",  ScheduleId },  // 0 = новое расписание
                    {
                        "unitName",
                        UnitName ?? string.Empty  // Берём из выбранного UnitModel
                    },
                { "idMedicine", MedicineId }
            };
            await Shell.Current.GoToAsync(route, parameters);
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

                // Сначала запрашиваем подтверждение у пользователя
                bool confirmDelete = await Shell.Current.DisplayAlertAsync(
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить это лекарство?",
                    "Да",
                    "Нет"
                );

                // Если пользователь нажал "Нет" — выходим из метода без удаления
                if (!confirmDelete)
                {
                    return;
                }

                // Если пользователь нажал "Да" — выполняем удаление
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