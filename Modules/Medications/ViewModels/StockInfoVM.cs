using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Entities;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;
using System.Diagnostics;
using MedicinesTracker.Constants;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(StockId), "idStock")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class StockInfoVM : CreationStepBaseVM
    {
        private readonly IStockRepository _stockRepository;
        private readonly IValidatorService _validatorService;
        private readonly IMedicineBuilder _medicineBuilder;
        private readonly IMedicationCreationNavigationService _medicationNavigation;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private Stock _stock = new();

        [ObservableProperty]
        private int _stockId;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private string? _unitName;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();

        [ObservableProperty]
        private string _currentQuantityError = string.Empty;

        [ObservableProperty]
        private string _thresholdError = string.Empty;

        [ObservableProperty]
        private bool _hasCurrentQuantityError;

        [ObservableProperty]
        private bool _hasThresholdError;

        private bool _isInitialized = false;

        public StockInfoVM(
            IStockRepository stockRepository,
            IValidatorService validatorService,
            IMedicineBuilder medicineBuilder,
            StepManager stepManager,
            IMedicationCreationNavigationService medicationNavigation,
            INavigationService navigation) : base(stepManager, navigation)
        {
            _stockRepository = stockRepository;
            _validatorService = validatorService;
            _medicineBuilder = medicineBuilder;
            _medicationNavigation = medicationNavigation;
            _navigation = navigation;
        }

        public override async Task ContinueAsync()
        {
            await SaveMedicine();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                if (!IsEditingExisting)
                {
                    if (_stepManager.CurrentStep != 2)
                        _stepManager.CurrentStep = 2;
                }

                await LoadDataAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StockInfoVM ERROR] {ex.Message}");
            }
        }

        partial void OnStockIdChanged(int value)
        {
            IsEditingExisting = value > 0;
            UpdateButtonState();
            if (value >= 0 && !_isInitialized)
            {
                _isInitialized = true;
                _ = LoadDataAsync();
            }
        }

        private void UpdateButtonState()
        {
            SaveButtonState.Text = IsEditingExisting ? "Сохранить" : "Далее";
            SaveButtonState.IsPrimary = true;
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                Debug.WriteLine($"LoadData: IsEditingExisting={IsEditingExisting}, StockId={StockId}");

                if (IsEditingExisting && StockId > 0)
                {
                    await LoadStockDataAsync(StockId);
                }
                else
                {
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}");
            }
        }

        private void ResetForm()
        {
            Stock = new Stock
            {
                CurrentQuantity = null,
                Threshold = null
            };
            OnPropertyChanged(nameof(Stock));
        }

        [RelayCommand]
        private void ValidateCurrentQuantity(string text)
        {
            var (isValid, errorMessage, value) = _validatorService.ValidatePositiveInt(text, "Количество", ValidationConstants.MaxQuantity);

            CurrentQuantityError = errorMessage;
            HasCurrentQuantityError = !isValid;

            if (isValid && value.HasValue)
            {
                Stock.CurrentQuantity = value.Value;
            }
            else
            {
                Stock.CurrentQuantity = null;
            }
        }

        [RelayCommand]
        private void ValidateThreshold(string text)
        {
            var (isValid, errorMessage, value) = _validatorService.ValidatePositiveInt(text, "Порог", ValidationConstants.MaxQuantity);

            ThresholdError = errorMessage;
            HasThresholdError = !isValid;

            if (isValid && value.HasValue)
            {
                Stock.Threshold = value.Value;
            }
            else
            {
                Stock.Threshold = null;
            }
        }

        private bool ValidateStockData(out string errorMessage)
        {
            if (HasCurrentQuantityError || HasThresholdError)
            {
                errorMessage = "Исправьте ошибки в полях перед сохранением";
                return false;
            }

            var errors = _validatorService.GetStockValidationErrors(Stock);
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
                if (!ValidateStockData(out var error))
                {
                    await _navigation.ShowAlertAsync("Ошибка", error);
                    return;
                }

                if (IsEditingExisting && StockId > 0)
                {
                    Debug.WriteLine("Режим: Редактирование существующего запаса");
                    IsBusy = true;

                    var stockToSave = new Stock
                    {
                        IdStock = Stock.IdStock,
                        IdMedicine = Stock.IdMedicine,
                        CurrentQuantity = Stock.CurrentQuantity.Value,
                        Threshold = Stock.Threshold.Value,
                        ReminderEnabled = Stock.ReminderEnabled
                    };

                    var rowsAffected = await _stockRepository.UpdateStockAsync(stockToSave);
                    if (rowsAffected > 0)
                    {
                        await _navigation.ShowAlertAsync("Успех", "Запас обновлен");
                        await _navigation.GoBackAsync();
                    }
                }
                else if (!IsEditingExisting)
                {
                    Debug.WriteLine("Режим: Добавление запаса через Builder");

                    var state = _medicineBuilder.GetState();
                    if (state.Medicine == null)
                    {
                        await _navigation.ShowAlertAsync("Ошибка",
                            "Сначала заполните основную информацию лекарства");
                        return;
                    }

                    var stockForBuilder = new Stock
                    {
                        CurrentQuantity = Stock.CurrentQuantity!.Value,
                        Threshold = Stock.Threshold!.Value,
                        ReminderEnabled = Stock.ReminderEnabled
                    };

                    _medicineBuilder.WithStockInfo(stockForBuilder);

                    Debug.WriteLine($"StockInfo добавлен в Builder. Статус: {_medicineBuilder.IsComplete}");

                    _stepManager.NextStep();
                    await OpenMedicineSchedulePage();
                }
                else
                {
                    await _navigation.ShowAlertAsync("Ошибка",
                        "Невозможно сохранить: не указаны данные");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpenMedicineSchedulePage()
        {
            try
            {
                if (!IsEditingExisting)
                {
                    Debug.WriteLine($"StockInfoVM: Переходим к созданию расписания для нового лекарства");
                    await _medicationNavigation.ToScheduleTypeSelectionAsync(medicineId: 0, isNewMedicine: true);
                }
                else if (IsEditingExisting && MedicineId > 0)
                {
                    Debug.WriteLine($"StockInfoVM: Переходим к созданию расписания для существующего лекарства ID={MedicineId}");
                    await _medicationNavigation.ToScheduleTypeSelectionAsync(MedicineId, isNewMedicine: false);
                }
                else
                {
                    await _navigation.ShowAlertAsync("Ошибка",
                        "Не указано лекарство для создания расписания");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка перехода: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await _navigation.ShowAlertAsync("Ошибка",
                    $"Не удалось перейти к созданию расписания: {ex.Message}");
            }
        }

        private async Task LoadStockDataAsync(int stockId)
        {
            try
            {
                var stock = await _stockRepository.GetStockByIdAsync(stockId);
                if (stock != null)
                {
                    Stock = stock;

                    if (stock.CurrentQuantity.HasValue)
                    {
                        ValidateCurrentQuantity(stock.CurrentQuantity.Value.ToString());
                    }

                    if (stock.Threshold.HasValue)
                    {
                        ValidateThreshold(stock.Threshold.Value.ToString());
                    }
                }
                else
                {
                    await _navigation.ShowAlertAsync("Ошибка", "Запас лекарства не найден");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных запаса: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные запаса: {ex.Message}");
            }
        }

        public override async Task BackAsync()
        {
            _stepManager.PreviousStep();
            await _navigation.GoBackAsync();
        }
    }
}