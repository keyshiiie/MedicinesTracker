using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
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
        private string _loadingMessage = "Сохранение...";

        public StockInfoVM(IStockRepository stockRepository,
            IValidatorService validatorService)
        {
            _stockRepository = stockRepository;
            _validatorService = validatorService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MedicineListVM ERROR] {ex.Message}");
            }
        }

        partial void OnStockIdChanged(int value)
        {
            Debug.WriteLine($"OnMedicineIdChanged вызван со значением: {value}");

            IsEditingExisting = value > 0;

            if (value >= 0 && !_isInitialized)
            {
                _isInitialized = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                Debug.WriteLine($"LoadData вызван. Режим: {(IsEditingExisting ? "Редактирование" : "Добавление")}");

                // Если это редактирование, загружаем данные лекарства
                if (IsEditingExisting && StockId > 0)
                {
                    await LoadMedicineDataAsync(StockId);
                }
                else
                {
                    // Если это добавление, сбрасываем модель
                    ResetForm();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка",
                    $"Не удалось загрузить данные: {ex.Message}",
                    "OK");
            }
        }

        private void ResetForm()
        {
            // Сбрасываем модель для нового лекарства
            Stock = new StockModel();
            OnPropertyChanged(nameof(Stock));

            Debug.WriteLine("Форма сброшена для добавления нового запаса лекарства");
        }

        [RelayCommand]
        private async Task SaveMedicine()
        {
            if (MedicineId == 0) return;
            try
            {
                var errors = _validatorService.GetStockValidationErrors(Stock);

                if (errors.Any())
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                // Блокируем UI на время сохранения
                IsBusy = true;
                int rowsAffected;

                // Совместная логика сохранения/редактирования
                if (IsEditingExisting && StockId > 0)
                {
                    // Редактирование существующего
                    Debug.WriteLine("Режим: Редактирование существующего запаса лекарства");
                    rowsAffected = await _stockRepository.UpdateStockAsync(Stock);
                    // Возвращаемся назад
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Добавление нового
                    Debug.WriteLine("Режим: Добавление нового запаса лекарства");
                    rowsAffected = await _stockRepository.AddStockAsync(Stock, MedicineId);
                    if (rowsAffected > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await OpenMedicineSchedulePage();
                        });
                    }
                }

                Debug.WriteLine($"Результат операции: {rowsAffected} строк затронуто");

                if (rowsAffected > 0)
                {
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
                if (MedicineId <= 0) return;

                Debug.WriteLine($"Переход к созданию расписания для MedicineId={MedicineId}");

                var route = "MedicineSchedulePage";
                var parameters = new Dictionary<string, object>
                {
                    { "idSchedule", 0 },
                    { "unitName", UnitName ?? string.Empty },
                    { "idMedicine", MedicineId }
                };

                // Используем анимированный переход
                await Shell.Current.GoToAsync(route, true, parameters);
            }
            catch (Exception ex)
            {
                // Можно показать сообщение или вернуться назад
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка",
                    "Не удалось перейти к созданию расписания",
                    "ОК");
                await Shell.Current.GoToAsync("..");
            }
        }

        private async Task LoadMedicineDataAsync(int stockId)
        {
            try
            {
                Debug.WriteLine($"Загрузка данных запаса лекарства с ID: {stockId}");

                // Загружаем данные лекарства по ID
                var stock = await _stockRepository.GetStockByIdAsync(stockId);

                if (stock != null)
                {
                    Stock = stock;
                }
                else
                {
                    Debug.WriteLine($"Запас с ID {stockId} не найден");
                    await Shell.Current.DisplayAlertAsync(
                        "Ошибка",
                        "Запас лекарства не найден",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных запаса лекарства: {ex.Message}");
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка",
                    $"Не удалось загрузить данные запаса лекарства: {ex.Message}",
                    "OK");
            }
        }
    }
}
