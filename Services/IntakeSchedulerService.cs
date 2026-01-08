using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MedicinesTracker.Services
{
    public class IntakeSchedulerService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IIntakeRepository _intakeRepository;
        private const int DAYS_AHEAD = 7;

        public IntakeSchedulerService(
            IIntakeRepository intakeRepository,
            IMedicineRepository medicineRepository)
        {
            _intakeRepository = intakeRepository;
            _medicineRepository = medicineRepository;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // проверяем, когда последняя генерация была
                var lastGenerationDate = Preferences.Get("LastIntakeGeneration", DateTime.MinValue);
                var today = DateTime.Today;

                if (lastGenerationDate < today)
                {
                    Debug.WriteLine($"Генерация записей на {DAYS_AHEAD} дней вперед");

                    // генерация на неделю вперёд
                    await GenerateIntakesForPeriod(today, today.AddDays(DAYS_AHEAD - 1));

                    // сохраняем дату генерации
                    Preferences.Set("LastIntakeGeneration", today);

                    Debug.WriteLine("Записи успешно сгенерированы");
                }
                else
                {
                    Debug.WriteLine("Записи уже сгенерированы сегодня");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации планировщика: {ex.Message}");
            }
        }

        public async Task CheckAndUpdateAsync()
        {
            try
            {
                var today = DateTime.Today;
                var lastCheck = Preferences.Get("LastSchedulerCheck", DateTime.MinValue);

                // не чаще раза в день проверяем
                if (lastCheck.Date < today)
                {
                    // проверяем, есть ли записи на сегодня
                    var todayIntakes = await _intakeRepository.GetIntakesByDateAsync(today.ToString("yyyy-MM-dd"));

                    if (!todayIntakes.Any())
                    {
                        Debug.WriteLine("Нет записей на сегодня, генерируем...");
                        await GenerateIntakesForPeriod(today, today);
                    }

                    // Проверяем, нужно ли генерировать на будущие дни
                    await EnsureFutureIntakes();

                    Preferences.Set("LastSchedulerCheck", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка проверки планировщика: {ex.Message}");
            }
        }

        private async Task EnsureFutureIntakes()
        {
            var today = DateTime.Today;
            var lastGenerationDate = Preferences.Get("LastIntakeGeneration", DateTime.MinValue);

            // если последняя генерация была более 3 дней назад, то генерируем на неделю
            if ((today - lastGenerationDate).Days >= 3)
            {
                await GenerateIntakesForPeriod(today, today.AddDays(DAYS_AHEAD - 1));
                Preferences.Set("LastIntakeGeneration", today);
            }
        }

        private async Task GenerateIntakesForPeriod(DateTime startDate, DateTime endDate)
        {
            var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();

            foreach (var medicine in medicines)
            {
                // Парсим времена приема
                var times = ParseTimes(medicine.Times);

                foreach (var time in times)
                {
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        if (ShouldTakeMedicine(medicine, date))
                        {
                            // Упрощенный IdScheduleTime (можно улучшить)
                            var scheduleTimeId = 1;
                            if (!string.IsNullOrEmpty(medicine.TimeOrders))
                            {
                                var orders = medicine.TimeOrders.Split(',', StringSplitOptions.RemoveEmptyEntries);
                                var timeIndex = Array.IndexOf(times, time);
                                if (timeIndex >= 0 && timeIndex < orders.Length)
                                {
                                    int.TryParse(orders[timeIndex], out scheduleTimeId);
                                }
                            }

                            await CreateIntakeForMedicine(medicine, date, time, scheduleTimeId);
                        }
                    }
                }
            }
        }

        private string[] ParseTimes(string? timesString) // Делаем параметр nullable
        {
            if (string.IsNullOrEmpty(timesString))
                return new[] { "08:00" }; // Время по умолчанию

            return timesString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        private bool ShouldTakeMedicine(MedicineWithScheduleDto medicine, DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            // Если расписание неактивно
            if (!medicine.ScheduleIsActive)
                return false;

            if (medicine.ScheduleTypeCode == "ONETIME")
            {
                // Одноразовый прием
                return medicine.OneTimeDate == dateStr;
            }
            else if (medicine.ScheduleTypeCode == "RECURRING")
            {
                // Проверяем период действия
                if (!string.IsNullOrEmpty(medicine.DateStart))
                {
                    var startDate = DateTime.Parse(medicine.DateStart); // Используем Parse без параметра
                    if (date < startDate)
                        return false;
                }

                if (!string.IsNullOrEmpty(medicine.DateEnd))
                {
                    var endDate = DateTime.Parse(medicine.DateEnd);
                    if (date > endDate)
                        return false;
                }

                if (medicine.ScheduleModeCode == "INTERVAL" && medicine.DaysInterval.HasValue)
                {
                    // Логика интервального приема
                    DateTime referenceDate;
                    if (!string.IsNullOrEmpty(medicine.OneTimeDate))
                    {
                        referenceDate = DateTime.Parse(medicine.OneTimeDate);
                    }
                    else if (!string.IsNullOrEmpty(medicine.DateStart))
                    {
                        referenceDate = DateTime.Parse(medicine.DateStart);
                    }
                    else
                    {
                        return false;
                    }

                    var daysDiff = (date - referenceDate).Days;
                    return daysDiff % medicine.DaysInterval.Value == 0;
                }
                else if (medicine.ScheduleModeCode == "WEEKDAYS" && !string.IsNullOrEmpty(medicine.WeekDayIds))
                {
                    // Логика приема по дням недели
                    var dayNumber = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var dayIds = medicine.WeekDayIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var num) ? num : 0)
                        .Where(id => id > 0);

                    return dayIds.Contains(dayNumber);
                }
            }

            return false;
        }

        private async Task CreateIntakeForMedicine(MedicineWithScheduleDto medicine, DateTime date, string time, int scheduleTimeId)
        {
            try
            {
                // Проверяем, нет ли уже записи
                var existing = await _intakeRepository.GetIntakeByMedicineAndDateAsync(
                    medicine.IdMedicine,
                    date.ToString("yyyy-MM-dd"));

                if (existing == null)
                {
                    // Создаем новую запись
                    var intake = new IntakeModel
                    {
                        IdMedicine = medicine.IdMedicine,
                        IdSchedule = medicine.IdSchedule,
                        IdScheduleTime = scheduleTimeId,
                        IsCompleted = false,
                        Date = date.ToString("yyyy-MM-dd"),
                        Time = time,
                        ActualDosage = medicine.Dosage
                    };

                    await _intakeRepository.AddIntakeAsync(intake);
                    Debug.WriteLine($"Создана запись: {medicine.MedicineName} - {date:yyyy-MM-dd} {time}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка создания записи для {medicine.MedicineName}: {ex.Message}");
            }
        }

        // Метод для удаления старых записей (опционально)
        public async Task CleanupOldIntakesAsync(int keepDays = 30)
        {
            try
            {
                var cutoffDate = DateTime.Today.AddDays(-keepDays);

                // Удаляем записи старше keepDays дней
                var deletedCount = await _intakeRepository.DeleteFutureIntakesAsync(cutoffDate);

                if (deletedCount > 0)
                {
                    Debug.WriteLine($"Удалено {deletedCount} старых записей приема");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка очистки старых записей: {ex.Message}");
            }
        }

        // Метод для принудительной перегенерации
        public async Task RegenerateIntakesAsync(int daysAhead = 7)
        {
            try
            {
                var today = DateTime.Today;

                // Удаляем будущие записи
                await _intakeRepository.DeleteFutureIntakesAsync(today);

                // Генерируем заново
                await GenerateIntakesForPeriod(today, today.AddDays(daysAhead - 1));

                Preferences.Set("LastIntakeGeneration", today);
                Debug.WriteLine($"Перегенерированы записи на {daysAhead} дней вперед");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка перегенерации записей: {ex.Message}");
            }
        }
    }
}