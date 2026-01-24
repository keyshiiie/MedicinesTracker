using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IIntakeSchedulerService
    {
        Task GenerateTodayIntakesAsync();
        Task RegenerateIntakesForMedicineAsync(int medicineId);
        Task CheckAndUpdateAsync();
        Task RegenerateAllFutureIntakesAsync();
    }

    public class IntakeSchedulerService : IIntakeSchedulerService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IScheduleTimeRepository _scheduleTimeRepository;
        private readonly IIntakeRepository _intakeRepository;

        public IntakeSchedulerService(
            IMedicineRepository medicineRepository,
            IScheduleTimeRepository scheduleTimeRepository,
            IIntakeRepository intakeRepository)
        {
            _medicineRepository = medicineRepository;
            _scheduleTimeRepository = scheduleTimeRepository;
            _intakeRepository = intakeRepository;
        }

        public async Task GenerateTodayIntakesAsync()
        {
            try
            {
                Debug.WriteLine("=== Генерация записей приема НА СЕГОДНЯ ===");

                var today = DateTime.Today;

                // Получаем все активные лекарства с расписанием
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();

                Debug.WriteLine($"Найдено активных лекарств: {medicineList.Count}");

                foreach (var medicine in medicineList)
                {
                    if (!medicine.ScheduleIsActive) continue;

                    await GenerateIntakesForMedicineOnDateAsync(medicine, today);
                }

                Debug.WriteLine("✅ Записи приема на сегодня сгенерированы");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка генерации записей: {ex.Message}");
            }
        }

        public async Task RegenerateIntakesForMedicineAsync(int medicineId)
        {
            try
            {
                Debug.WriteLine($"=== Перегенерация записей для лекарства ID: {medicineId} ===");

                // Удаляем будущие записи (с сегодняшнего дня)
                await _intakeRepository.DeleteFutureIntakesForMedicineAsync(medicineId, DateTime.Today);

                // Получаем лекарство с расписанием
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicine = medicines.FirstOrDefault(m => m.IdMedicine == medicineId);

                if (medicine == null || !medicine.ScheduleIsActive)
                {
                    Debug.WriteLine($"Лекарство ID:{medicineId} не активно или не найдено");
                    return;
                }

                // Генерируем записи ТОЛЬКО НА СЕГОДНЯ
                await GenerateIntakesForMedicineOnDateAsync(medicine, DateTime.Today);

                Debug.WriteLine($"✅ Записи для лекарства {medicineId} перегенерированы (только на сегодня)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка перегенерации: {ex.Message}");
            }
        }

        private async Task GenerateIntakesForMedicineOnDateAsync(MedicineWithScheduleDto medicine, DateTime date)
        {
            try
            {
                var dateStr = date.ToString("yyyy-MM-dd");

                // Проверяем, нужно ли принимать лекарство в эту дату
                if (!ShouldTakeMedicineOnDate(medicine, date))
                {
                    Debug.WriteLine($"Лекарство {medicine.MedicineName} не нужно принимать {dateStr}");
                    return;
                }

                Debug.WriteLine($"Генерация для {medicine.MedicineName} на {dateStr}");

                // Разбираем времена приема из строки
                var times = ParseTimes(medicine.Times);

                foreach (var time in times)
                {
                    if (string.IsNullOrEmpty(time)) continue;

                    // Получаем IdScheduleTime для этого времени
                    var scheduleTime = await _scheduleTimeRepository.GetScheduleTimeByScheduleAndTimeAsync(
                        medicine.IdSchedule, time);

                    if (scheduleTime == null)
                    {
                        Debug.WriteLine($"Не найден ScheduleTime для расписания {medicine.IdSchedule} и времени {time}");
                        continue;
                    }

                    if (!scheduleTime.IsActive) continue;

                    // Проверяем, существует ли уже запись
                    var exists = await _intakeRepository.IntakeExistsAsync(
                        medicine.IdMedicine,
                        dateStr,
                        time);

                    if (!exists)
                    {
                        // Создаем новую запись
                        var intakeModel = new IntakeModel
                        {
                            IdMedicine = medicine.IdMedicine,
                            IdSchedule = medicine.IdSchedule,
                            IdScheduleTime = scheduleTime.IdTime,
                            Date = dateStr,
                            Time = time,
                            ActualDosage = medicine.Dosage,
                            IsCompleted = false,
                            TakenDateTime = null
                        };

                        var intakeId = await _intakeRepository.AddIntakeAsync(intakeModel);
                        Debug.WriteLine($"✅ Создана запись: {medicine.MedicineName} - {dateStr} {time} (ID: {intakeId})");
                    }
                    else
                    {
                        Debug.WriteLine($"Запись уже существует: {medicine.MedicineName} - {dateStr} {time}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка генерации для {medicine.MedicineName} на {date:yyyy-MM-dd}: {ex.Message}");
            }
        }

        private string[] ParseTimes(string? timesString)
        {
            if (string.IsNullOrEmpty(timesString))
                return new string[0];

            return timesString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        private bool ShouldTakeMedicineOnDate(MedicineWithScheduleDto medicine, DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            if (medicine.ScheduleTypeCode == "ONETIME")
            {
                return medicine.OneTimeDate == dateStr;
            }
            else if (medicine.ScheduleTypeCode == "RECURRING")
            {
                // Проверяем период действия
                if (!string.IsNullOrEmpty(medicine.DateStart))
                {
                    var startDate = DateTime.Parse(medicine.DateStart);
                    if (date < startDate) return false;
                }

                if (!string.IsNullOrEmpty(medicine.DateEnd))
                {
                    var endDate = DateTime.Parse(medicine.DateEnd);
                    if (date > endDate) return false;
                }

                if (medicine.ScheduleModeCode == "INTERVAL" && medicine.DaysInterval.HasValue)
                {
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
                    var dayNumber = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var dayIds = medicine.WeekDayIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var num) ? num : 0)
                        .Where(id => id > 0);

                    return dayIds.Contains(dayNumber);
                }

                // Если режим не указан, принимается каждый день
                return true;
            }

            return false;
        }

        public async Task CheckAndUpdateAsync()
        {
            // Проверяем и генерируем записи на сегодня
            await GenerateTodayIntakesAsync();
        }

        public async Task RegenerateAllFutureIntakesAsync()
        {
            try
            {
                Debug.WriteLine("=== Перегенерация всех записей НА СЕГОДНЯ ===");

                // Удаляем все будущие записи (с сегодняшнего дня)
                await _intakeRepository.DeleteFutureIntakesAsync(DateTime.Today);

                // Получаем все активные лекарства
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();

                // Генерируем ТОЛЬКО НА СЕГОДНЯ
                foreach (var medicine in medicineList)
                {
                    await GenerateIntakesForMedicineOnDateAsync(medicine, DateTime.Today);
                }

                Debug.WriteLine("✅ Все записи на сегодня перегенерированы");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка перегенерации всех записей: {ex.Message}");
            }
        }
    }
}