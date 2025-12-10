using CommunityToolkit.Mvvm.ComponentModel;
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
        private MedicineDetailDto _medicine;

        public StockInfoVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
            // Инициализируем пустым объектом на случай, если параметр не передан
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
    }
}
