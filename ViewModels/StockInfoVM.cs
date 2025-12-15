using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class StockInfoVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineDetailDto _medicine = new();

        [ObservableProperty]
        private StockModel _newStock = new();
        public StockInfoVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        partial void OnMedicineChanged(MedicineDetailDto value)
        {
            if (value != null)
            {
                Debug.WriteLine($"Получены данные лекарства: {Medicine.MedicineName}");
            }
            else
            {
                Debug.WriteLine("Предупреждение: medicine равен null");
            }
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                //if (NewStock.InitialQuantity >= 120 || NewStock.Threshold >= 120 || NewStock.CurrentQuantity >= 120)
                //{
                //    await Shell.Current.DisplayAlert(
                //            "Предупреждение!",
                //            "Числовые значения не могут вревышать лимит 120.",
                //            "ОК");
                //    return;
                //}
                NewStock.IdStock = Medicine.IdStock;

                NewStock.Threshold = Medicine.Threshold;

                NewStock.CurrentQuantity = Medicine.CurrentQuantity;

                NewStock.ReminderEnabled = Medicine.ReminderEnabled;

                // Вызываем сервис для сохранения
                var rowsAffected = await _medicineService.EditStockAsync(NewStock);

                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        "Запас успешно обновлён!",
                        "ОК");
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Запас не был обновлен",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка!",
                    $"Не удалось сохранить: {ex.Message}",
                    "ОК");
            }
        }
    }
}
