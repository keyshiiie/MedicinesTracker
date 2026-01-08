using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MedicinesTracker.Modules.Notifications.ViewModels
{
    public partial class TodayMedicineVM : ObservableObject
    {
        private readonly IScheduleService _scheduleService;
        private readonly IIntakeRepository _intakeRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IntakeSchedulerService? _schedulerService; // Делаем nullable

        [ObservableProperty]
        private ObservableCollection<GroupedTodayMedicine> _groupedMedicines = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        public TodayMedicineVM(
            IScheduleService scheduleService,
            IIntakeRepository intakeRepository,
            IStockRepository stockRepository,
            IntakeSchedulerService? schedulerService = null)
        {
            _scheduleService = scheduleService;
            _intakeRepository = intakeRepository;
            _schedulerService = schedulerService;
            _stockRepository = stockRepository;
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

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                // Можно добавить обновление записей перед загрузкой
                // await _schedulerService.CheckAndUpdateAsync();

                var todayMedicines = await _intakeRepository.GetTodayMedicineAsync();

                // Группируем по получателю
                var grouped = todayMedicines
                    .GroupBy(m => m.RecipientName)
                    .Select(g => new GroupedTodayMedicine(g.Key, g.OrderBy(m => m.OrderInDay)))
                    .ToList();

                GroupedMedicines.Clear();
                foreach (var group in grouped)
                {
                    GroupedMedicines.Add(group);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки лекарств на сегодня: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task MarkIntakeAsync(TodayMedicineDto medicine)
        {
            Debug.WriteLine($"MarkIntakeAsync вызван! Medicine: {medicine?.MedicineName}");

            if (medicine == null)
            {
                Debug.WriteLine("Medicine is null!");
                return;
            }

            // Проверяем, есть ли уже запись в Intake
            if (medicine.IdIntake == 0)
            {
                await Shell.Current.DisplayAlertAsync("Внимание",
                    "Сначала нужно создать запись о приеме", "OK");
                return;
            }

            try
            {
                // Проверяем, наступило ли время приема
                if (!IsTimeForIntake(medicine.Time))
                {
                    // Показываем модальное окно с предупреждением
                    var result = await ShowTimeWarningDialog(medicine);

                    if (!result)
                    {
                        Debug.WriteLine("Пользователь отменил отметку приёма");
                        return;
                    }
                }

                Debug.WriteLine($"Начинаем отметку приёма для: {medicine.MedicineName}, IdIntake: {medicine.IdIntake}");

                // Создаем модель для обновления
                var intakeModel = new IntakeModel
                {
                    IdIntake = medicine.IdIntake,
                    IsCompleted = true,
                    Date = DateTime.Now.ToString("yyyy-MM-dd"),
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    IdMedicine = medicine.IdMedicine,
                    IdSchedule = medicine.IdSchedule,
                    IdScheduleTime = medicine.IdScheduleTime,
                    ActualDosage = medicine.Dosage
                };

                // Получаем текущий запас лекарства
                var stock = await _stockRepository.GetStockByIdAsync(medicine.IdStock);

                if (stock == null)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось найти информацию о запасе лекарства", "OK");
                    return;
                }

                // Проверяем, достаточно ли лекарства
                if (stock.CurrentQuantity < medicine.Dosage)
                {
                    await Shell.Current.DisplayAlertAsync("Внимание",
                        $"Недостаточно лекарства. Остаток: {stock.CurrentQuantity}, требуется: {medicine.Dosage}",
                        "OK");
                    return;
                }

                // Рассчитываем новое количество
                int newQuantity = stock.CurrentQuantity - medicine.Dosage;

                // Обновляем запись о приеме
                var rowsAffected = await _intakeRepository.UpdateIntakeAsync(intakeModel);

                Debug.WriteLine($"Приём отмечен. Затронуто строк: {rowsAffected}");

                if (rowsAffected > 0)
                {
                    // Обновляем количество в запасе
                    await _stockRepository.UpdateCurrentQuantityAsync(medicine.IdStock, newQuantity);

                    await Shell.Current.DisplayAlertAsync("Успех",
                        $"Приём лекарства отмечен. Остаток: {newQuantity}",
                        "OK");

                    // Обновляем данные после отметки
                    await LoadDataAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось обновить запись о приеме", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", ex.Message, "OK");
            }
        }

        private bool IsTimeForIntake(string scheduledTime)
        {
            try
            {
                // Парсим запланированное время (формат "HH:mm" или "HH:mm:ss")
                var timeParts = scheduledTime.Split(':');
                if (timeParts.Length >= 2)
                {
                    if (int.TryParse(timeParts[0], out int scheduledHour) &&
                        int.TryParse(timeParts[1], out int scheduledMinute))
                    {
                        var now = DateTime.Now;
                        var scheduledDateTime = new DateTime(
                            now.Year, now.Month, now.Day,
                            scheduledHour, scheduledMinute, 0);

                        // Сравниваем текущее время с запланированным
                        return now >= scheduledDateTime;
                    }
                }

                // Если не удалось распарсить, считаем что время наступило
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка проверки времени: {ex.Message}");
                return true;
            }
        }

        private async Task<bool> ShowTimeWarningDialog(TodayMedicineDto medicine)
        {
            var result = await Shell.Current.DisplayAlertAsync(
                "Время приема еще не наступило",
                $"Запланированное время приема: {medicine.Time}\n" +
                $"Текущее время: {DateTime.Now:HH:mm}\n\n" +
                "Вы уверены, что хотите отметить прием сейчас?",
                "Да, отметить",
                "Отмена");

            return result;
        }
    }
}