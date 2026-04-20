using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Entities;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Constants;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    public partial class BaseInfoVM : CreationStepBaseVM
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IReferencesDataRepository _referencesDataRepository;
        private readonly IRecipientRepository _recipientRepository;
        private readonly IValidatorService _validatorService;
        private readonly IMedicineBuilder _medicineBuilder;
        private readonly IMedicationCreationNavigationService _medicationNavigation;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private Medicine _medicine = new();

        [ObservableProperty]
        private ObservableCollection<Unit> _units = new();

        [ObservableProperty]
        private Unit? _selectedUnit;

        [ObservableProperty]
        private ObservableCollection<Recipient> _recipients = new();

        [ObservableProperty]
        private Recipient? _selectedRecipient;

        [ObservableProperty]
        private ObservableCollection<MethodAdmission> _methodAdmissions = new();

        [ObservableProperty]
        private MethodAdmission? _selectedMethodAdmission;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();

        private bool _isInitialized = false;

        public BaseInfoVM(
            IReferencesDataRepository referencesDataRepository,
            IMedicineRepository medicineRepository,
            IRecipientRepository recipientRepository,
            IValidatorService validatorService,
            IMedicineBuilder medicineBuilder,
            StepManager stepManager,
            IMedicationCreationNavigationService medicationNavigation,
            INavigationService navigation) : base(stepManager, navigation)
        {
            _referencesDataRepository = referencesDataRepository;
            _medicineRepository = medicineRepository;
            _recipientRepository = recipientRepository;
            _validatorService = validatorService;
            _medicineBuilder = medicineBuilder;
            _medicationNavigation = medicationNavigation;
            _navigation = navigation;
        }

        public override async Task ContinueAsync()
        {
            await SaveMedicine();
        }

        partial void OnMedicineIdChanged(int value)
        {
            IsEditingExisting = value > 0;
            UpdateButtonState();
            _ = LoadDataAsync();
        }

        private void UpdateButtonState()
        {
            SaveButtonState.Text = IsEditingExisting ? "Сохранить" : "Далее";
            SaveButtonState.IsPrimary = true;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                if (!IsEditingExisting)
                {
                    _stepManager.Reset();
                }

                await LoadDataAsync();
                _isInitialized = true;
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

                    if (!IsEditingExisting)
                    {
                        _medicineBuilder.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}");
            }
        }

        private void ResetForm()
        {
            Medicine = new Medicine();
            SelectedUnit = null;
            SelectedRecipient = null;
            SelectedMethodAdmission = null;
        }

        private async Task LoadReferenceData()
        {
            await Task.WhenAll(
                LoadUnitsAsync(),
                LoadRecipientsAsync(),
                LoadMethodsAsync()
            );
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
                    await _navigation.ShowAlertAsync("Ошибка", "Лекарство не найдено");
                }
            }
            catch (Exception ex)
            {
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные лекарства: {ex.Message}");
            }
        }

        private async Task LoadUnitsAsync()
        {
            try
            {
                var units = await _referencesDataRepository.GetAllUnitsAsync();
                Units = new ObservableCollection<Unit>(units);
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
                Recipients = new ObservableCollection<Recipient>(recipients);
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
                MethodAdmissions = new ObservableCollection<MethodAdmission>(methods);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки способов приёма: {ex.Message}");
            }
        }

        partial void OnSelectedUnitChanged(Unit? value)
        {
            if (value != null) Medicine.IdUnit = value.IdUnit;
        }

        partial void OnSelectedRecipientChanged(Recipient? value)
        {
            if (value != null) Medicine.IdRecipient = value.IdRecipient;
        }

        partial void OnSelectedMethodAdmissionChanged(MethodAdmission? value)
        {
            if (value != null) Medicine.IdMethodAdmission = value.IdMethodAdmission;
        }

        private bool ValidateBaseInfo(out string errorMessage)
        {
            var errors = _validatorService.GetBaseInfoValidationErrors(
                Medicine, SelectedUnit, SelectedRecipient, SelectedMethodAdmission);

            if (errors.Any())
            {
                errorMessage = string.Join("\n", errors);
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                if (!ValidateBaseInfo(out var error))
                {
                    await _navigation.ShowAlertAsync("Ошибка", error);
                    return;
                }

                if (IsEditingExisting && MedicineId > 0)
                {
                    await UpdateExistingMedicine();
                }
                else
                {
                    await CreateNewMedicine();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}");
            }
        }

        private async Task UpdateExistingMedicine()
        {
            Debug.WriteLine("Режим: Редактирование существующего лекарства");
            Medicine.IdMedicine = MedicineId;
            var rowsAffected = await _medicineRepository.UpdateMedicineAsync(Medicine);

            if (rowsAffected > 0)
            {
                await _navigation.ShowAlertAsync("Успех", "Данные обновлены");
                await _navigation.GoBackAsync();
            }
        }

        private async Task CreateNewMedicine()
        {
            Debug.WriteLine("Режим: Добавление нового лекарства через Builder");
            _medicineBuilder.WithBaseInfo(Medicine);
            Debug.WriteLine($"BaseInfo добавлен в Builder. Статус: {_medicineBuilder.IsComplete}");

            _stepManager.NextStep();
            await OpenStockPage();
        }

        [RelayCommand]
        private async Task OpenStockPage()
        {
            try
            {
                if (IsEditingExisting && MedicineId > 0)
                {
                    await _medicationNavigation.ToStockInfoAsync(MedicineId, SelectedUnit?.Name ?? string.Empty, 0);
                }
                else
                {
                    await _medicationNavigation.ToStockInfoAsync(MedicineId, SelectedUnit?.Name ?? string.Empty, null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу запасов: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось перейти к запасам: {ex.Message}");
            }
        }
    }
}