using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    public partial class BaseInfoVM : ObservableObject
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IReferencesDataRepository _referencesDataRepository;
        private readonly IRecipientRepository _recipientRepository;

        [ObservableProperty]
        private MedicineModel _medicine = new();

        [ObservableProperty]
        private ObservableCollection<UnitModel> _units = new();

        [ObservableProperty]
        private UnitModel? _selectedUnit;

        [ObservableProperty]
        private ObservableCollection<RecipientModel> _recipients = new();

        [ObservableProperty]
        private RecipientModel? _selectedRecipient;

        [ObservableProperty]
        private ObservableCollection<MethodAdmissionModel> _methodAdmissions = new();

        [ObservableProperty]
        private MethodAdmissionModel? _selectedMethodAdmission;

        [ObservableProperty]
        private bool _isEditingExisting;

        [ObservableProperty]
        private int _medicineId;

        private bool _isInitialized = false;

        public BaseInfoVM(IReferencesDataRepository referencesDataRepository, 
            IMedicineRepository medicineRepository,
            IRecipientRepository recipientRepository)
        {
            _referencesDataRepository = referencesDataRepository;
            _medicineRepository = medicineRepository;
            _recipientRepository = recipientRepository;
        }

        // Обработка изменения MedicineId
        partial void OnMedicineIdChanged(int value)
        {
            IsEditingExisting = value > 0;

            if (value >= 0 && !_isInitialized)
            {
                _isInitialized = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadData();
                });
            }
        }

        [RelayCommand]
        public async Task LoadData()
        {
            try
            {
                // Загружаем справочные данные только если они еще не загружены
                if (!Units.Any() || !MethodAdmissions.Any() || !Recipients.Any())
                {
                    await LoadReferenceData();
                }

                // Если это редактирование, загружаем данные лекарства
                if (IsEditingExisting && MedicineId > 0)
                {
                    await LoadMedicineDataAsync(MedicineId);
                }
                else
                {
                    // Если это добавление, сбрасываем модель
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка",
                    $"Не удалось загрузить данные: {ex.Message}",
                    "OK");
            }
        }

        private void ResetForm()
        {
            // Сбрасываем модель для нового лекарства
            Medicine = new MedicineModel();

            // Сбрасываем выбранные значения в пикерах
            SelectedUnit = null;
            SelectedRecipient = null;
            SelectedMethodAdmission = null;
        }

        private async Task LoadReferenceData()
        {
            await LoadUnitsAsync();
            await LoadRecipientsAsync();
            await LoadMethodsAsync();
        }

        private async Task LoadMedicineDataAsync(int medicineId)
        {
            try
            {
                // Загружаем данные лекарства по ID
                var medicine = await _medicineRepository.GetMedicineByIdAsync(medicineId);

                if (medicine != null)
                {
                    Medicine = medicine;

                    // Устанавливаем выбранные значения в пикерах
                    if (Units.Any())
                    {
                        SelectedUnit = Units.FirstOrDefault(u => u.IdUnit == Medicine.IdUnit);
                    }

                    if (Recipients.Any())
                    {
                        SelectedRecipient = Recipients.FirstOrDefault(r => r.IdRecipient == Medicine.IdRecipient);
                    }

                    if (MethodAdmissions.Any())
                    {
                        SelectedMethodAdmission = MethodAdmissions.FirstOrDefault(m => m.IdMethodAdmission == Medicine.IdMethodAdmission);
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Лекарство не найдено",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка",
                    $"Не удалось загрузить данные лекарства: {ex.Message}",
                    "OK");
            }
        }

        private async Task LoadUnitsAsync()
        {
            try
            {
                var units = await _referencesDataRepository.GetAllUnitsAsync();
                Units = new ObservableCollection<UnitModel>(units);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки единиц измерения: {ex.Message}");
            }
        }

        private async Task LoadRecipientsAsync()
        {
            try
            {
                var recipients = await _recipientRepository.GetAllRecipientsAsync();
                Recipients = new ObservableCollection<RecipientModel>(recipients);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки получателей: {ex.Message}");
            }
        }

        private async Task LoadMethodsAsync()
        {
            try
            {
                var methods = await _referencesDataRepository.GetAllMethodsAdmissionAsync();
                MethodAdmissions = new ObservableCollection<MethodAdmissionModel>(methods);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки способов приёма: {ex.Message}");
            }
        }

        // Обработчики изменений в пикерах
        partial void OnSelectedUnitChanged(UnitModel? value)
        {
            if (value != null)
            {
                Medicine.IdUnit = value.IdUnit;
            }
        }

        partial void OnSelectedRecipientChanged(RecipientModel? value)
        {
            if (value != null)
            {
                Medicine.IdRecipient = value.IdRecipient;
            }
        }

        partial void OnSelectedMethodAdmissionChanged(MethodAdmissionModel? value)
        {
            if (value != null)
            {
                Medicine.IdMethodAdmission = value.IdMethodAdmission;
            }
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(Medicine.Name))
                {
                    Debug.WriteLine("Ошибка: Название лекарства не может быть пустым");
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Название лекарства не может быть пустым",
                        "OK");
                    return;
                }

                if (Medicine.IdUnit == 0)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать единицу измерения");
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Необходимо выбрать единицу измерения",
                        "OK");
                    return;
                }

                if (Medicine.IdRecipient == 0)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать получателя");
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Необходимо выбрать получателя",
                        "OK");
                    return;
                }

                if (Medicine.IdMethodAdmission == 0)
                {
                    Debug.WriteLine("Ошибка: Необходимо выбрать способ приёма");
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Необходимо выбрать способ приёма",
                        "OK");
                    return;
                }

                int rowsAffected;
                string successMessage;

                // Совместная логика сохранения/редактирования
                if (IsEditingExisting && MedicineId > 0)
                {
                    // Редактирование существующего
                    Debug.WriteLine("Режим: Редактирование существующего лекарства");
                    Medicine.IdMedicine = MedicineId;
                    rowsAffected = await _medicineRepository.UpdateMedicineAsync(Medicine);
                    successMessage = "Лекарство успешно обновлено!";
                    // Возвращаемся назад
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Добавление
                    try
                    {
                        var newId = await _medicineRepository.AddMedicineAsync(Medicine);
                        if (newId > 0)
                        {
                            Medicine.IdMedicine = newId;
                            MedicineId = newId;
                            rowsAffected = 1;
                        }
                        else
                        {
                            rowsAffected = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка получения ID: {ex.Message}");
                        rowsAffected = 0;
                    }

                    successMessage = "Лекарство успешно добавлено!";
                    await OpenStockPage();
                }

                Debug.WriteLine($"Результат операции: {rowsAffected} строк затронуто");

                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        successMessage,
                        "ОК");

                    Debug.WriteLine("Сохранение успешно, возврат назад");


                }
                else
                {
                    Debug.WriteLine("Предупреждение: Операция не затронула ни одной строки");
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Операция не была выполнена",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Исключение при сохранении: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка!",
                    $"Не удалось сохранить: {ex.Message}",
                    "ОК");
            }
        }

        [RelayCommand]
        private async Task OpenStockPage()
        {
            try
            {
                var route = "StockInfoPage";
                var parameters = new Dictionary<string, object>
                {
                    { "idStock", 0 },  // 0 = новый запас
                    {
                        "unitName",
                        SelectedUnit?.Name ?? string.Empty  // Берём из выбранного UnitModel
                    },
                    { "idMedicine", MedicineId }
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу запасов: {ex.Message}");
            }
        }


        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}