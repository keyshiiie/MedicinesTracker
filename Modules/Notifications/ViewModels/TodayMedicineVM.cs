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
        private readonly IIntakeRepository _intakeRepository;
        private readonly IStockRepository _stockRepository;
        private readonly INotificationPlannerService _notificationPlanner;

        [ObservableProperty]
        private ObservableCollection<TodayMedicineDto> _medicines = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        public TodayMedicineVM(
            IIntakeRepository intakeRepository,
            IStockRepository stockRepository,
            INotificationPlannerService notificationPlanner)
        {
            _intakeRepository = intakeRepository;
            _stockRepository = stockRepository;
            _notificationPlanner = notificationPlanner;
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
            IsRefreshing = false;
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var todayMedicines = await _intakeRepository.GetTodayMedicineAsync();

                Debug.WriteLine($"=== Загрузка данных на сегодня ===");
                Debug.WriteLine($"Получено записей: {todayMedicines.Count()}");

                foreach (var m in todayMedicines)
                {
                    Debug.WriteLine($"  - {m.RecipientName}: {m.MedicineName} в {m.Time}");
                }

                // Сортируем по времени
                var sortedMedicines = todayMedicines
                    .OrderBy(m => m.OrderInDay)
                    .ToList();

                Medicines.Clear();
                foreach (var medicine in sortedMedicines)
                {
                    Medicines.Add(medicine);
                }

                Debug.WriteLine($"Загружено лекарств: {Medicines.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task MarkIntakeAsync(TodayMedicineDto medicine)
        {
            try
            {
                Debug.WriteLine($"=== MarkIntakeAsync вызван для {medicine?.MedicineName} ===");

                if (medicine == null)
                {
                    Debug.WriteLine("Ошибка: medicine = null");
                    return;
                }

                if (!IsTimeForIntake(medicine.Time))
                {
                    var confirm = await Shell.Current.DisplayAlertAsync(
                        "Внимание",
                        $"Запланированное время приема: {medicine.Time}\n" +
                        $"Текущее время: {DateTime.Now:HH:mm}\n\n" +
                        "Вы уверены, что хотите отметить прием сейчас?",
                        "Да, отметить",
                        "Отмена");

                    if (!confirm) return;
                }

                // Получаем запись приема
                var intake = await _intakeRepository.GetIntakeByMedicineAndDateTimeAsync(
                    medicine.IdMedicine,
                    DateTime.Today.ToString("yyyy-MM-dd"),
                    medicine.Time);

                if (intake == null)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Запись приема не найдена", "OK");
                    return;
                }

                if (intake.IsCompleted)
                {
                    await Shell.Current.DisplayAlertAsync("Информация", "Этот прием уже был отмечен", "OK");
                    return;
                }

                var stock = await _stockRepository.GetStockByIdAsync(medicine.IdStock);
                if (stock == null)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось найти информацию о запасе", "OK");
                    return;
                }

                // Проверяем, что CurrentQuantity не null
                if (!stock.CurrentQuantity.HasValue)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Информация о количестве лекарства отсутствует",
                        "OK");
                    return;
                }

                if (stock.CurrentQuantity.Value < medicine.Dosage)
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        $"Недостаточно лекарства. Остаток: {stock.CurrentQuantity.Value}, требуется: {medicine.Dosage}",
                        "OK");
                    return;
                }

                // Сохраняем название и дозировку для сообщения
                string medicineName = medicine.MedicineName;
                int dosage = medicine.Dosage;
                string unitName = medicine.UnitName;
                int newQuantity = stock.CurrentQuantity.Value - dosage;

                // Обновляем intake
                intake.IsCompleted = true;
                intake.TakenDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var rowsAffected = await _intakeRepository.UpdateIntakeAsync(intake);

                if (rowsAffected > 0)
                {
                    await _stockRepository.UpdateCurrentQuantityAsync(medicine.IdStock, newQuantity);

                    // Отменяем уведомление
                    await _notificationPlanner.CancelNotificationForIntakeAsync(intake.IdIntake, medicine.IdMedicine, medicine.Time);

                    Debug.WriteLine($"✅ Прием отмечен. Остаток: {newQuantity}");

                    await Shell.Current.DisplayAlertAsync(
                        "Прием отмечен",
                        $"Лекарство: {medicineName}\n" +
                        $"Дозировка: {dosage} {unitName}\n" +
                        $"Остаток: {newQuantity} {unitName}",
                        "OK");

                    // Обновляем список
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось отметить прием", "OK");
            }
        }

        private bool IsTimeForIntake(string scheduledTime)
        {
            try
            {
                if (TimeSpan.TryParse(scheduledTime, out var scheduled))
                {
                    return DateTime.Now.TimeOfDay >= scheduled;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}