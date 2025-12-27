using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Repository;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(StockId), "idStock")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class StockInfoVM : ObservableObject
    {
        private readonly IStockRepository _stockRepository;

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

        public StockInfoVM(IStockRepository stockRepository)
        {
            _stockRepository = stockRepository;
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
                    await LoadData();
                });
            }
        }

        [RelayCommand]
        public async Task LoadData()
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
                int rowsAffected;
                string successMessage;

                // Совместная логика сохранения/редактирования
                if (IsEditingExisting && StockId > 0)
                {
                    // Редактирование существующего
                    Debug.WriteLine("Режим: Редактирование существующего запаса лекарства");
                    Stock.IdStock = StockId;
                    rowsAffected = await _stockRepository.UpdateStockAsync(Stock);
                    successMessage = "Запас лекарства успешно обновлен!";
                    // Возвращаемся назад
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Добавление нового
                    Debug.WriteLine("Режим: Добавление нового запаса лекарства");
                    rowsAffected = await _stockRepository.AddStockAsync(Stock, MedicineId);
                    successMessage = "Запас лекарства успешно добавлен!";
                    await OpenMedicineSchedulePage();
                    // await Shell.Current.GoToAsync("..");
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
        private async Task OpenMedicineSchedulePage()
        {
            if (MedicineId <= 0) return; // 0 или -1

            var route = "MedicineSchedulePage";
            var parameters = new Dictionary<string, object>
            {
                { "idSchedule", 0 },  // 0 = новое расписание
                    {
                        "unitName",
                        UnitName ?? string.Empty  // Берём из выбранного UnitModel
                    },
                { "idMedicine", MedicineId }
            };
            await Shell.Current.GoToAsync(route, parameters);
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
