using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IScheduleService
    {
        Task<int> SaveScheduleAsync(MedicineScheduleDto scheduleDto,
                                   List<WeekDayModel> selectedDays,
                                   List<TimeSpan> selectedTimes);
        Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId);
        Task ScheduleNotificationsForMedicineAsync(int medicineId);
    }

    public class ScheduleService : IScheduleService
    {
        private readonly IMedicineScheduleRepository _scheduleRepository;
        private readonly IScheduleTimeRepository _timeRepository;
        private readonly IScheduleWeekDaysRepository _weekDaysRepository;
        private readonly INotificationPlannerService _notificationPlanner; // переименовано
        private readonly IIntakeGeneratorService _intakeGenerator; // переименовано

        public ScheduleService(
            IMedicineScheduleRepository scheduleRepository,
            IScheduleTimeRepository timeRepository,
            IScheduleWeekDaysRepository weekDaysRepository,
            INotificationPlannerService notificationPlanner, // переименовано
            IIntakeGeneratorService intakeGenerator) // переименовано
        {
            _scheduleRepository = scheduleRepository;
            _timeRepository = timeRepository;
            _weekDaysRepository = weekDaysRepository;
            _notificationPlanner = notificationPlanner;
            _intakeGenerator = intakeGenerator;
        }

        public async Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId)
        {
            return await _scheduleRepository.GetMedicineScheduleById(scheduleId);
        }

        public async Task<int> SaveScheduleAsync(
            MedicineScheduleDto scheduleDto,
            List<WeekDayModel> selectedDays,
            List<TimeSpan> selectedTimes)
        {
            int scheduleId;
            bool isUpdate = scheduleDto.IdSchedule > 0;

            var scheduleModel = ConvertToScheduleModel(scheduleDto);

            if (isUpdate)
            {
                await _scheduleRepository.UpdateMedicineScheduleAsync(scheduleModel);
                scheduleId = scheduleDto.IdSchedule;
                await _timeRepository.DeleteScheduleTimesAsync(scheduleId);
                await _weekDaysRepository.DeleteScheduleWeekDaysAsync(scheduleId);
            }
            else
            {
                scheduleId = await _scheduleRepository.AddMedicineShedule(scheduleModel);
            }

            if (selectedTimes.Any())
            {
                await SaveScheduleTimes(scheduleId, selectedTimes);
            }

            if (selectedDays.Any() && scheduleDto.ScheduleModeCode == "WEEKDAYS")
            {
                await SaveScheduleWeekDays(scheduleId, selectedDays);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"=== Сохранение расписания для лекарства ID: {scheduleDto.IdMedicine} ===");

                    await _intakeGenerator.RegenerateIntakesForMedicineAsync(scheduleDto.IdMedicine);
                    Debug.WriteLine($"✅ Записи приема перегенерированы для лекарства ID: {scheduleDto.IdMedicine}");

                    await Task.Delay(500);

                    if (_notificationPlanner != null)
                    {
                        _notificationPlanner.CancelAll();
                        await _notificationPlanner.PlanForTodayAsync();
                        Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {scheduleDto.IdMedicine}");
                    }
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

                if (_notificationPlanner != null)
                {
                    _notificationPlanner.CancelAll();
                    await _notificationPlanner.PlanForTodayAsync();
                    Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {medicineId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка планирования уведомлений: {ex.Message}");
            }
        }

        private async Task SaveScheduleWeekDays(int scheduleId, List<WeekDayModel> selectedDays)
        {
            foreach (var day in selectedDays)
            {
                var weekDays = new ScheduleWeekDaysModel
                {
                    IdSchedule = scheduleId,
                    IdDay = day.IdDay
                };
                await _weekDaysRepository.UpdateScheduleWeekDayAsync(weekDays);
            }
        }

        private async Task SaveScheduleTimes(int scheduleId, List<TimeSpan> selectedTimes)
        {
            int order = 1;
            foreach (var time in selectedTimes.OrderBy(t => t))
            {
                var timeModel = new ScheduleTimeModel
                {
                    IdSchedule = scheduleId,
                    Time = time.ToString(@"hh\:mm"),
                    OrderInDay = order++,
                    IsActive = true
                };

                await _timeRepository.AddScheduleTimeAsync(timeModel);
            }
        }

        private MedicineScheduleModel ConvertToScheduleModel(MedicineScheduleDto scheduleDto)
        {
            return new MedicineScheduleModel
            {
                IdSchedule = scheduleDto.IdSchedule,
                IdMedicine = scheduleDto.IdMedicine,
                IdScheduleType = scheduleDto.IdScheduleType,
                IdScheduleMode = scheduleDto.IdScheduleMode,
                IdRecurrencePattern = scheduleDto.IdRecurrencePattern,
                OneTimeDate = scheduleDto.OneTimeDate,
                Dosage = scheduleDto.Dosage,
                DateStart = scheduleDto.DateStart,
                DateEnd = scheduleDto.DateEnd,
                IsActive = scheduleDto.ScheduleIsActive
            };
        }
    }
}
