using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(StockId), "idStock")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class StockInfoVM : ObservableObject
    {
        private readonly IStockRepository _stockRepository;
        private readonly IValidatorService _validatorService;
        private readonly IMedicineBuilder _medicineBuilder;

        [ObservableProperty]
        private StockModel _stock = new();

        [ObservableProperty]
        private int _stockId;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private string? _unitName;

        [ObservableProperty]
        private bool _isEditingExisting;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();

        // Свойства для валидации
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
            IMedicineBuilder medicineBuilder)
        {
            _stockRepository = stockRepository;
            _validatorService = validatorService;
            _medicineBuilder = medicineBuilder;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
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
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadDataAsync();
                });
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
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }

        private void ResetForm()
        {
            Stock = new StockModel
            {
                CurrentQuantity = null, // Явно устанавливаем null
                Threshold = null         // Явно устанавливаем null
            };
            OnPropertyChanged(nameof(Stock));
        }

        // Команда для валидации текущего количества
        [RelayCommand]
        private void ValidateCurrentQuantity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                CurrentQuantityError = "Введите количество";
                HasCurrentQuantityError = true;
                Stock.CurrentQuantity = null; // Устанавливаем null при пустом поле
                return;
            }

            if (!int.TryParse(text, out int value))
            {
                CurrentQuantityError = "Введите целое число";
                HasCurrentQuantityError = true;
                Stock.CurrentQuantity = null;
                return;
            }

            if (value < 0)
            {
                CurrentQuantityError = "Количество не может быть отрицательным";
                HasCurrentQuantityError = true;
                Stock.CurrentQuantity = null;
                return;
            }

            if (value > 1000)
            {
                CurrentQuantityError = "Количество не может быть больше 1000";
                HasCurrentQuantityError = true;
                Stock.CurrentQuantity = null;
                return;
            }

            // Если все проверки пройдены
            CurrentQuantityError = string.Empty;
            HasCurrentQuantityError = false;
            Stock.CurrentQuantity = value;
        }

        // Команда для валидации порога
        [RelayCommand]
        private void ValidateThreshold(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                ThresholdError = "Введите порог";
                HasThresholdError = true;
                Stock.Threshold = null; // Устанавливаем null при пустом поле
                return;
            }

            if (!int.TryParse(text, out int value))
            {
                ThresholdError = "Введите целое число";
                HasThresholdError = true;
                Stock.Threshold = null;
                return;
            }

            if (value < 0)
            {
                ThresholdError = "Порог не может быть отрицательным";
                HasThresholdError = true;
                Stock.Threshold = null;
                return;
            }

            if (value > 1000)
            {
                ThresholdError = "Порог не может быть больше 1000";
                HasThresholdError = true;
                Stock.Threshold = null;
                return;
            }

            // Если все проверки пройдены
            ThresholdError = string.Empty;
            HasThresholdError = false;
            Stock.Threshold = value;
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                if (HasCurrentQuantityError || HasThresholdError)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Исправьте ошибки в полях перед сохранением", "OK");
                    return;
                }

                var errors = _validatorService.GetStockValidationErrors(Stock);
                if (errors.Any())
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                if (IsEditingExisting && StockId > 0)
                {
                    Debug.WriteLine("Режим: Редактирование существующего запаса");
                    IsBusy = true;

                    // Создаем копию с non-null значениями для репозитория
                    var stockToSave = new StockModel
                    {
                        IdStock = Stock.IdStock,
                        IdMedicine = Stock.IdMedicine,
                        CurrentQuantity = Stock.CurrentQuantity!.Value, 
                        Threshold = Stock.Threshold!.Value,
                        ReminderEnabled = Stock.ReminderEnabled
                    };

                    var rowsAffected = await _stockRepository.UpdateStockAsync(stockToSave);
                    if (rowsAffected > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Запас обновлен", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else if (!IsEditingExisting)
                {
                    Debug.WriteLine("Режим: Добавление запаса через Builder");

                    var state = _medicineBuilder.GetState();
                    if (state.Medicine == null)
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка",
                            "Сначала заполните основную информацию лекарства", "OK");
                        return;
                    }

                    var stockForBuilder = new StockModel
                    {
                        CurrentQuantity = Stock.CurrentQuantity!.Value,
                        Threshold = Stock.Threshold!.Value,
                        ReminderEnabled = Stock.ReminderEnabled
                    };

                    _medicineBuilder.WithStockInfo(stockForBuilder);

                    Debug.WriteLine($"StockInfo добавлен в Builder. Статус: {_medicineBuilder.IsComplete}");

                    await OpenMedicineSchedulePage();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Невозможно сохранить: не указаны данные", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сохранить: {ex.Message}", "ОК");
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
                var parameters = new Dictionary<string, object>();

                // Для добавления нового лекарства через Builder
                if (!IsEditingExisting)
                {
                    // Переходим к выбору типа расписания
                    parameters.Add("isNewMedicine", true);
                    // MedicineId будет 0 при создании нового лекарства
                    parameters.Add("medicineId", 0);

                    Debug.WriteLine($"StockInfoVM: Переходим к созданию расписания для нового лекарства");
                    await Shell.Current.GoToAsync("ScheduleTypeSelectionPage", parameters);
                }
                else if (IsEditingExisting && MedicineId > 0)
                {
                    // Для редактирования существующего лекарства
                    parameters.Add("medicineId", MedicineId);
                    parameters.Add("isNewMedicine", false);

                    Debug.WriteLine($"StockInfoVM: Переходим к созданию расписания для существующего лекарства ID={MedicineId}");
                    await Shell.Current.GoToAsync("ScheduleTypeSelectionPage", parameters);
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Не указано лекарство для создания расписания", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка перехода: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось перейти к созданию расписания: {ex.Message}", "ОК");
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

                    // Валидируем загруженные значения
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
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Запас лекарства не найден", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных запаса: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить данные запаса: {ex.Message}", "OK");
            }
        }
    }
}