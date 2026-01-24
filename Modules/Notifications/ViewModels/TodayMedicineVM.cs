using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using Plugin.LocalNotification;
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
        private readonly INotificationSchedulerService _notificationService;
        private readonly IIntakeSchedulerService _schedulerService;

        [ObservableProperty]
        private ObservableCollection<GroupedTodayMedicine> _groupedMedicines = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        // В TodayMedicineVM.cs обновите конструктор:
        public TodayMedicineVM(
            IScheduleService scheduleService,
            IIntakeRepository intakeRepository,
            IStockRepository stockRepository,
            IIntakeSchedulerService schedulerService,
            INotificationSchedulerService notificationService) // Добавьте этот параметр
        {
            _scheduleService = scheduleService;
            _intakeRepository = intakeRepository;
            _schedulerService = schedulerService;
            _stockRepository = stockRepository;
            _notificationService = notificationService; // Сохраните
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

                Debug.WriteLine($"Начинаем отметку приёма для: {medicine.MedicineName}");

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
                var todayDate = DateTime.Now.ToString("yyyy-MM-dd");

                // Если IdIntake = 0, значит записи нет, создаем ее
                if (medicine.IdIntake == 0)
                {
                    var intakeModel = new IntakeModel
                    {
                        IdMedicine = medicine.IdMedicine,
                        IdSchedule = medicine.IdSchedule,
                        IdScheduleTime = medicine.IdScheduleTime,
                        Date = todayDate,
                        Time = DateTime.Now.ToString("HH:mm"),
                        TakenDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ActualDosage = medicine.Dosage,
                        IsCompleted = true
                    };

                    medicine.IdIntake = await _intakeRepository.AddIntakeAsync(intakeModel);
                    Debug.WriteLine($"Создана новая запись приема: ID={medicine.IdIntake}");
                }
                else
                {
                    // Обновляем существующую запись
                    var intakeModel = new IntakeModel
                    {
                        IdIntake = medicine.IdIntake,
                        IsCompleted = true,
                        Date = todayDate, // Используем сегодняшнюю дату
                        Time = DateTime.Now.ToString("HH:mm"),
                        TakenDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ActualDosage = medicine.Dosage,
                        IdMedicine = medicine.IdMedicine,
                        IdSchedule = medicine.IdSchedule,
                        IdScheduleTime = medicine.IdScheduleTime
                    };

                    var rowsAffected = await _intakeRepository.UpdateIntakeAsync(intakeModel);
                    Debug.WriteLine($"Приём отмечен. Затронуто строк: {rowsAffected}");
                }

                // Обновляем количество в запасе
                await _stockRepository.UpdateCurrentQuantityAsync(medicine.IdStock, newQuantity);

                // Перепланируем уведомления (отменяем для этого приема)
                if (_notificationService != null)
                {
                    await _notificationService.CancelNotificationForIntakeAsync(medicine.IdIntake);
                }

                await Shell.Current.DisplayAlertAsync("Успех",
                    $"Приём лекарства отмечен. Остаток: {newQuantity}",
                    "OK");

                // Обновляем список
                await LoadDataAsync();
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