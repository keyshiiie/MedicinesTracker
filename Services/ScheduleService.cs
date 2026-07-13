using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IScheduleService
    {
        Task<int> SaveScheduleAsync(MedicineScheduleDto scheduleDto,
                                   List<WeekDay> selectedDays,
                                   List<TimeSpan> selectedTimes);
        Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId);
        Task ScheduleNotificationsForMedicineAsync(int medicineId);
    }

    public class ScheduleService : IScheduleService
    {
        private readonly IMedicineScheduleRepository _scheduleRepository;
        private readonly IScheduleTimeRepository _timeRepository;
        private readonly IScheduleWeekDaysRepository _weekDaysRepository;
        private readonly INotificationPlannerService _notificationPlanner;
        private readonly IIntakeGeneratorService _intakeGenerator;
        private readonly IIntakeRepository _intakeRepository; // 👈 ДОБАВИТЬ

        public ScheduleService(
            IMedicineScheduleRepository scheduleRepository,
            IScheduleTimeRepository timeRepository,
            IScheduleWeekDaysRepository weekDaysRepository,
            INotificationPlannerService notificationPlanner,
            IIntakeGeneratorService intakeGenerator,
            IIntakeRepository intakeRepository) // 👈 ДОБАВИТЬ
        {
            _scheduleRepository = scheduleRepository;
            _timeRepository = timeRepository;
            _weekDaysRepository = weekDaysRepository;
            _notificationPlanner = notificationPlanner;
            _intakeGenerator = intakeGenerator;
            _intakeRepository = intakeRepository; // 👈 ДОБАВИТЬ
        }

        public async Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId)
        {
            return await _scheduleRepository.GetMedicineScheduleById(scheduleId);
        }

        public async Task<int> SaveScheduleAsync(
            MedicineScheduleDto scheduleDto,
            List<WeekDay> selectedDays,
            List<TimeSpan> selectedTimes)
        {
            int scheduleId;
            bool isUpdate = scheduleDto.IdSchedule > 0;

            var schedule = ConvertToSchedule(scheduleDto);

            if (isUpdate)
            {
                // 👇 ВАЖНО: Сначала удаляем будущие записи приема
                // Это нужно сделать ДО удаления ScheduleTime, чтобы избежать ошибки внешнего ключа
                await _intakeRepository.DeleteFutureIntakesForMedicineAsync(
                    scheduleDto.IdMedicine, DateTime.Today);

                await _scheduleRepository.UpdateMedicineScheduleAsync(schedule);
                scheduleId = scheduleDto.IdSchedule;

                // Теперь можно безопасно удалять старые данные
                await _timeRepository.DeleteScheduleTimesAsync(scheduleId);
                await _weekDaysRepository.DeleteScheduleWeekDaysAsync(scheduleId);
            }
            else
            {
                scheduleId = await _scheduleRepository.AddMedicineSchedule(schedule);
            }

            // Сохраняем время приема
            if (selectedTimes.Any())
            {
                await SaveScheduleTimes(scheduleId, selectedTimes);
            }

            // Сохраняем дни недели (только для WEEKDAYS режима)
            if (selectedDays.Any() && scheduleDto.ScheduleModeCode == "WEEKDAYS")
            {
                await SaveScheduleWeekDays(scheduleId, selectedDays);
            }

            // Фоновая задача для перегенерации записей и уведомлений
            _ = Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"=== Сохранение расписания для лекарства ID: {scheduleDto.IdMedicine} ===");

                    // Генерируем новые записи на сегодня
                    await _intakeGenerator.RegenerateIntakesForMedicineAsync(scheduleDto.IdMedicine);
                    Debug.WriteLine($"✅ Записи приема перегенерированы для лекарства ID: {scheduleDto.IdMedicine}");

                    await Task.Delay(500);

                    _notificationPlanner.CancelAll();
                    await _notificationPlanner.PlanForTodayAsync();
                    Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {scheduleDto.IdMedicine}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка: {ex.Message}");
                }
            });

            return scheduleId;
        }

        public async Task ScheduleNotificationsForMedicineAsync(int medicineId)
        {
            try
            {
                Debug.WriteLine($"Планируем уведомления для лекарства ID: {medicineId}");

                _notificationPlanner.CancelAll();
                await _notificationPlanner.PlanForTodayAsync();
                Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {medicineId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка планирования уведомлений: {ex.Message}");
            }
        }

        private async Task SaveScheduleWeekDays(int scheduleId, List<WeekDay> selectedDays)
        {
            foreach (var day in selectedDays)
            {
                var scheduleWeekDay = new ScheduleWeekDay
                {
                    IdSchedule = scheduleId,
                    IdDay = day.IdDay
                };
                await _weekDaysRepository.AddScheduleWeekDayAsync(scheduleWeekDay);
            }
        }

        private async Task SaveScheduleTimes(int scheduleId, List<TimeSpan> selectedTimes)
        {
            int order = 1;
            foreach (var time in selectedTimes.OrderBy(t => t))
            {
                var scheduleTime = new ScheduleTime
                {
                    IdSchedule = scheduleId,
                    Time = time.ToString(@"hh\:mm"),
                    OrderInDay = order++,
                    IsActive = true
                };

                await _timeRepository.AddScheduleTimeAsync(scheduleTime);
            }
        }

        private MedicationSchedule ConvertToSchedule(MedicineScheduleDto dto)
        {
            return new MedicationSchedule
            {
                IdSchedule = dto.IdSchedule,
                IdMedicine = dto.IdMedicine,
                IdScheduleType = dto.IdScheduleType,
                IdScheduleMode = dto.IdScheduleMode,
                IdRecurrencePattern = dto.IdRecurrencePattern,
                OneTimeDate = dto.OneTimeDate,
                Dosage = dto.Dosage,
                DateStart = dto.DateStart,
                DateEnd = dto.DateEnd,
                IsActive = dto.ScheduleIsActive
            };
        }
    }
}