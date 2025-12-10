using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class MedicineDetailVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineDetailDto _medicine;

        public MedicineDetailVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
            _medicine = new MedicineDetailDto();
        }

        // Обрабатываем полученное значение через QueryProperty
        partial void OnMedicineChanged(MedicineDetailDto value)
        {
            if (value != null)
            {
                // Обновляем свойство (ObservableProperty сделает NotifyPropertyChanged)
                Medicine = value;
                Debug.WriteLine($"Получены данные лекарства: {Medicine.MedicineName}");
            }
            else
            {
                Debug.WriteLine("Предупреждение: medicine равен null");
            }
        }

        [RelayCommand]
        private async Task OpenBaseInfoPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;
            try
            {
                var route = "BaseInfoPage";
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
        [RelayCommand]
        private async Task OpenNotificationPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;
            try
            {
                var route = "NotificationInfoPage";
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
        [RelayCommand]
        private async Task OpenStockPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;
            try
            {
                var route = "StockInfoPage";
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

    }
}
