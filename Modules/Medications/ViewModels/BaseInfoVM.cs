using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
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
        private readonly IValidatorService _validatorService;
        private readonly IMedicineBuilder _medicineBuilder;

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

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();

        public BaseInfoVM(
            IReferencesDataRepository referencesDataRepository,
            IMedicineRepository medicineRepository,
            IRecipientRepository recipientRepository,
            IValidatorService validatorService,
            IMedicineBuilder medicineBuilder)
        {
            _referencesDataRepository = referencesDataRepository;
            _medicineRepository = medicineRepository;
            _recipientRepository = recipientRepository;
            _validatorService = validatorService;
            _medicineBuilder = medicineBuilder;
        }

        partial void OnMedicineIdChanged(int value)
        {
            IsEditingExisting = value > 0;
            UpdateButtonState();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDataAsync();
            });
        }

        private void UpdateButtonState()
        {
            SaveButtonState.Text = IsEditingExisting ? "Сохранить" : "Далее";
            SaveButtonState.IsPrimary = true;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseInfoVM ERROR] {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                await LoadReferenceData();

                if (IsEditingExisting && MedicineId > 0)
                {
                    await LoadMedicineDataAsync(MedicineId);
                }
                else
                {
                    ResetForm();

                    // Если это добавление нового, сбрасываем builder
                    if (!IsEditingExisting)
                    {
                        _medicineBuilder.Reset();
                    }
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
            Medicine = new MedicineModel();
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
                var medicine = await _medicineRepository.GetMedicineByIdAsync(medicineId);

                if (medicine != null)
                {
                    Medicine = medicine;
                    SelectedUnit = Units.FirstOrDefault(u => u.IdUnit == Medicine.IdUnit);
                    SelectedRecipient = Recipients.FirstOrDefault(r => r.IdRecipient == Medicine.IdRecipient);
                    SelectedMethodAdmission = MethodAdmissions.FirstOrDefault(m => m.IdMethodAdmission == Medicine.IdMethodAdmission);
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Лекарство не найдено", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные лекарства: {ex.Message}", "OK");
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

        partial void OnSelectedUnitChanged(UnitModel? value)
        {
            if (value != null) Medicine.IdUnit = value.IdUnit;
        }

        partial void OnSelectedRecipientChanged(RecipientModel? value)
        {
            if (value != null) Medicine.IdRecipient = value.IdRecipient;
        }

        partial void OnSelectedMethodAdmissionChanged(MethodAdmissionModel? value)
        {
            if (value != null) Medicine.IdMethodAdmission = value.IdMethodAdmission;
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                var errors = _validatorService.GetBaseInfoValidationErrors(
                    Medicine, SelectedUnit, SelectedRecipient, SelectedMethodAdmission);

                if (errors.Any())
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                // РАЗДЕЛЕНИЕ ЛОГИКИ: РЕДАКТИРОВАНИЕ vs ДОБАВЛЕНИЕ
                if (IsEditingExisting && MedicineId > 0)
                {
                    // РЕДАКТИРОВАНИЕ СУЩЕСТВУЮЩЕГО - старая логика
                    Debug.WriteLine("Режим: Редактирование существующего лекарства");
                    Medicine.IdMedicine = MedicineId;
                    var rowsAffected = await _medicineRepository.UpdateMedicineAsync(Medicine);

                    if (rowsAffected > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Данные обновлены", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else
                {
                    // ДОБАВЛЕНИЕ НОВОГО - через Builder
                    Debug.WriteLine("Режим: Добавление нового лекарства через Builder");

                    // Сохраняем базовую информацию в Builder
                    _medicineBuilder.WithBaseInfo(Medicine);

                    Debug.WriteLine($"BaseInfo добавлен в Builder. Статус: {_medicineBuilder.IsComplete}");

                    // Переходим к следующему шагу
                    await OpenStockPage();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}", "ОК");
            }
        }

        [RelayCommand]
        private async Task OpenStockPage()
        {
            try
            {
                // Для редактирования передаем ID, для добавления - только unitName
                var parameters = new Dictionary<string, object>();

                if (IsEditingExisting && MedicineId > 0)
                {
                    // Редактирование: передаем ID
                    parameters.Add("idMedicine", MedicineId);
                    parameters.Add("idStock", 0); // 0 = новый запас
                }

                // Для обоих случаев передаем unitName
                parameters.Add("unitName", SelectedUnit?.Name ?? string.Empty);

                await Shell.Current.GoToAsync("StockInfoPage", parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу запасов: {ex.Message}");
            }
        }
    }
}