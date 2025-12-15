using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    [QueryProperty(nameof(Medicine), "medicine")]
    public partial class BaseInfoVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private MedicineDetailDto _medicine = new();

        [ObservableProperty]
        private MedicineModel _newMedicine = new();

        [ObservableProperty]
        private ObservableCollection<UnitModel> _units = new();

        [ObservableProperty]
        private UnitModel? _selectedUnit;

        public BaseInfoVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [ObservableProperty]
        private ObservableCollection<RecipientModel> _recipients = new();

        [ObservableProperty]
        private RecipientModel? _selectedRecipient;

        [ObservableProperty]
        private ObservableCollection<MethodAdmissionModel> _methodAdmissions = new();

        [ObservableProperty]
        private MethodAdmissionModel? _selectedMethodAdmission;


        // Обрабатываем полученное значение через QueryProperty
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
        public async Task LoadData()
        {
            try
            {
                await LoadUnitsAsync();
                await LoadRecipientsAsync();
                await LoadMethodsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                throw;
            }
        }

        private async Task LoadUnitsAsync()
        {
            var units = await _medicineService.GetAllUnitsAsync();
            Units = new ObservableCollection<UnitModel>(units);
            Debug.WriteLine($"Загружено единиц измерения: {Units.Count}");

            if (!string.IsNullOrEmpty(Medicine.UnitName))
            {
                SelectedUnit = Units.FirstOrDefault(u => u.Name == Medicine.UnitName);
            }
        }

        private async Task LoadRecipientsAsync()
        {
            var recipients = await _medicineService.GetAllRecipientsAsync();
            Recipients = new ObservableCollection<RecipientModel>(recipients);
            Debug.WriteLine($"Загружено получателей лекарств: {Recipients.Count}");

            if (!string.IsNullOrEmpty(Medicine.RecipientName))
            {
                SelectedRecipient = Recipients.FirstOrDefault(r => r.Name == Medicine.RecipientName);
            }
        }

        private async Task LoadMethodsAsync()
        {
            var methods = await _medicineService.GetAllMethodsAdmissionAsync();
            MethodAdmissions = new ObservableCollection<MethodAdmissionModel>(methods);
            Debug.WriteLine($"Загружено получателей лекарств: {MethodAdmissions.Count}");

            if (!string.IsNullOrEmpty(Medicine.MethodAdmissionName))
            {
                SelectedMethodAdmission = MethodAdmissions.FirstOrDefault(r => r.Name == Medicine.MethodAdmissionName);
            }
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(Medicine.MedicineName))
                {
                    Debug.WriteLine("Ошибка: Название лекарства не может быть пустым");
                    return;
                }

                if (SelectedUnit == null)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать единицу измерения");
                    return;
                }

                if (SelectedRecipient == null)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать получателя");
                    return;
                }

                if (SelectedMethodAdmission == null)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать способ приёма");
                    return;
                }

                // Заполняем DTO данными из ViewModel
                NewMedicine.IdMedicine = Medicine.IdMedicine;

                NewMedicine.Name = Medicine.MedicineName;

                NewMedicine.IdUnit = SelectedUnit.IdUnit;

                NewMedicine.IdRecipient = SelectedRecipient.IdRecipient;

                NewMedicine.IdMethodAdmission = SelectedMethodAdmission.IdMethodAdmission;

                // Вызываем сервис для сохранения
                var rowsAffected = await _medicineService.EditMedicineAsync(NewMedicine);

                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        "Лекарство успешно сохранено!",
                        "ОК");
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Лекарство не было обновлено",
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
