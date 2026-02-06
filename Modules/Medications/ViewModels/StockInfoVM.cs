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

        private bool _isInitialized = false;
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();
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
            SaveButtonState.Text = IsEditingExisting ? "Сохранить" : "Продолжить";
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
            Stock = new StockModel();
            OnPropertyChanged(nameof(Stock));
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            try
            {
                var errors = _validatorService.GetStockValidationErrors(Stock);
                if (errors.Any())
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                // РАЗДЕЛЕНИЕ ЛОГИКИ: РЕДАКТИРОВАНИЕ vs ДОБАВЛЕНИЕ
                if (IsEditingExisting && StockId > 0)
                {
                    // РЕДАКТИРОВАНИЕ - старая логика
                    Debug.WriteLine("Режим: Редактирование существующего запаса");
                    IsBusy = true;

                    var rowsAffected = await _stockRepository.UpdateStockAsync(Stock);
                    if (rowsAffected > 0)
                    {
                        await Shell.Current.DisplayAlertAsync("Успех", "Запас обновлен", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                }
                else if (!IsEditingExisting)
                {
                    // ДОБАВЛЕНИЕ - через Builder
                    Debug.WriteLine("Режим: Добавление запаса через Builder");

                    // Проверяем, есть ли базовая информация в Builder
                    var state = _medicineBuilder.GetState();
                    if (state.Medicine == null)
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка",
                            "Сначала заполните основную информацию лекарства", "OK");
                        return;
                    }

                    // Добавляем запас в Builder
                    _medicineBuilder.WithStockInfo(Stock);

                    Debug.WriteLine($"StockInfo добавлен в Builder. Статус: {_medicineBuilder.IsComplete}");

                    // Переходим к созданию расписания
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

                // Для добавления нового лекарства НЕ передаем MedicineId (его еще нет!)
                // Для редактирования передаем ID
                if (IsEditingExisting && MedicineId > 0)
                {
                    parameters.Add("idMedicine", MedicineId);
                    parameters.Add("idSchedule", 0); // 0 = новое расписание
                }
                else
                {
                    // Для добавления нового - передаем специальный флаг
                    parameters.Add("isNewMedicine", true);
                }

                // Для обоих случаев передаем unitName
                parameters.Add("unitName", UnitName ?? string.Empty);

                await Shell.Current.GoToAsync("MedicineSchedulePage", parameters);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось перейти к созданию расписания", "ОК");
                await Shell.Current.GoToAsync("..");
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