using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using System.Diagnostics;
using MedicinesTracker.Dto;

namespace MedicinesTracker.Services
{
    public interface IIntakeGeneratorService
    {
        Task GenerateTodayIntakesAsync();
        Task RegenerateIntakesForMedicineAsync(int medicineId);
        Task CheckAndUpdateAsync();
        Task RegenerateAllFutureIntakesAsync();
    }

    public class IntakeGeneratorService : IIntakeGeneratorService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IScheduleTimeRepository _scheduleTimeRepository;
        private readonly IIntakeRepository _intakeRepository;
        private readonly IScheduleEvaluator _scheduleEvaluator;
        private readonly AppDbContext _context;

        public IntakeGeneratorService(
            IMedicineRepository medicineRepository,
            IScheduleTimeRepository scheduleTimeRepository,
            IIntakeRepository intakeRepository,
            IScheduleEvaluator scheduleEvaluator,
            AppDbContext context)
        {
            _medicineRepository = medicineRepository;
            _scheduleTimeRepository = scheduleTimeRepository;
            _intakeRepository = intakeRepository;
            _scheduleEvaluator = scheduleEvaluator;
            _context = context;
        }

        public async Task GenerateTodayIntakesAsync()
        {
            await GenerateIntakesForDateAsync(DateTime.Today);
        }

        private async Task GenerateIntakesForDateAsync(DateTime date)
        {
            try
            {
                Debug.WriteLine($"=== Генерация записей приема на {date:yyyy-MM-dd} ===");

                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();

                Debug.WriteLine($"Найдено активных лекарств: {medicineList.Count}");

                foreach (var medicine in medicineList)
                {
                    await GenerateIntakesForMedicineOnDateAsync(medicine, date);
                }

                Debug.WriteLine($"✅ Записи приема на {date:yyyy-MM-dd} сгенерированы");
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

                // Получаем лекарство
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicine = medicines.FirstOrDefault(m => m.IdMedicine == medicineId);

                if (medicine == null || !medicine.ScheduleIsActive)
                {
                    Debug.WriteLine($"Лекарство ID:{medicineId} не активно или не найдено");
                    return;
                }

                // Генерируем записи на сегодня
                await GenerateIntakesForMedicineOnDateAsync(medicine, DateTime.Today);

                Debug.WriteLine($"✅ Записи для лекарства {medicineId} перегенерированы");
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
                // Проверяем, нужно ли принимать в эту дату
                if (!_scheduleEvaluator.ShouldTakeOnDate(medicine, date))
                {
                    Debug.WriteLine($"Лекарство {medicine.MedicineName} не нужно принимать {date:yyyy-MM-dd}");
                    return;
                }

                var dateStr = date.ToString("yyyy-MM-dd");
                var times = _scheduleEvaluator.GetMedicationTimes(medicine);

                Debug.WriteLine($"Генерация для {medicine.MedicineName} на {dateStr}, времен: {times.Length}");

                foreach (var time in times)
                {
                    if (string.IsNullOrEmpty(time)) continue;

                    // Получаем ScheduleTime
                    var scheduleTime = await _scheduleTimeRepository.GetScheduleTimeByScheduleAndTimeAsync(
                        medicine.IdSchedule, time);

                    if (scheduleTime == null || !scheduleTime.IsActive) continue;

                    // Ищем СУЩЕСТВУЮЩУЮ запись
                    var existingIntake = await _intakeRepository.GetIntakeByMedicineAndDateTimeAsync(
                        medicine.IdMedicine, dateStr, time);

                    if (existingIntake != null)
                    {
                        if (existingIntake.IsCompleted)
                        {
                            Debug.WriteLine($"⏰ Прием {medicine.MedicineName} в {time} уже отмечен как принятый");
                        }
                        else
                        {
                            Debug.WriteLine($"⏰ Прием {medicine.MedicineName} в {time} уже существует, ожидает отметки");
                        }
                        continue;
                    }

                    // Создаем новую запись
                    var intake = new Intake
                    {
                        IdMedicine = medicine.IdMedicine,
                        IdSchedule = medicine.IdSchedule,
                        IdScheduleTime = scheduleTime.IdTime,
                        Date = dateStr,
                        Time = time,
                        ActualDosage = medicine.Dosage,
                        IsCompleted = false
                    };

                    var intakeId = await _intakeRepository.AddIntakeAsync(intake);
                    Debug.WriteLine($"✅ Создана новая запись: {medicine.MedicineName} - {dateStr} {time}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка генерации для {medicine.MedicineName}: {ex.Message}");
            }
        }

        public async Task CheckAndUpdateAsync()
        {
            await GenerateTodayIntakesAsync();
        }

        public async Task RegenerateAllFutureIntakesAsync()
        {
            await GenerateTodayIntakesAsync();
        }
    }
}