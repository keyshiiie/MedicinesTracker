using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;

namespace MedicinesTracker.Services
{
    public interface IScheduleService
    {
        Task<int> SaveScheduleAsync(MedicineScheduleDto scheduleDto,
                                   List<WeekDayModel> selectedDays,
                                   List<TimeSpan> selectedTimes);
        Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId);
    }
    public class ScheduleService : IScheduleService
    {
        private readonly IMedicineScheduleRepository _scheduleRepository;
        private readonly IScheduleTimeRepository _timeRepository;
        private readonly IScheduleWeekDaysRepository _weekDaysRepository;

        public ScheduleService(
            IMedicineScheduleRepository scheduleRepository,
            IScheduleTimeRepository timeRepository,
            IScheduleWeekDaysRepository weekDaysRepository,
            IReferencesDataRepository referencesRepository)
        {
            _scheduleRepository = scheduleRepository;
            _timeRepository = timeRepository;
            _weekDaysRepository = weekDaysRepository;
        }
        public async Task<MedicineScheduleDto?> GetScheduleByIdAsync(int scheduleId)
        {
            return await _scheduleRepository.GetMedicineScheduleById(scheduleId);
        }
        public async Task<int> SaveScheduleAsync(MedicineScheduleDto scheduleDto,
            List<WeekDayModel> selectedDays, 
            List<TimeSpan> selectedTimes)
        {
            int scheduleId;
            bool isUpdate = scheduleDto.IdSchedule > 0;

            // 1. Конвертируем DTO в модель
            var scheduleModel = ConvertToScheduleModel(scheduleDto);

            if (isUpdate)
            {
                // 2. Обновляем расписание
                await _scheduleRepository.UpdateMedicineScheduleAsync(scheduleModel);
                scheduleId = scheduleDto.IdSchedule;

                // 3. Удаляем старые данные
                await _timeRepository.DeleteScheduleTimesAsync(scheduleId);
                await _weekDaysRepository.DeleteScheduleWeekDaysAsync(scheduleId);
            }
            else
            {
                // 2. Добавляем расписание
                scheduleId = await _scheduleRepository.AddMedicineShedule(scheduleModel);
            }

            // 4. Сохраняем время
            if (selectedTimes.Any())
            {
                await SaveScheduleTimes(scheduleId, selectedTimes);
            }

            // 5. Сохраняем дни
            if (selectedDays.Any() && scheduleDto.ScheduleModeCode == "WEEKDAYS")
            {
                await SaveScheduleWeekDays(scheduleId, selectedDays);
            }

            return scheduleId;
        }

        private async Task SaveScheduleWeekDays(int scheduleId, List<WeekDayModel> selectedDays)
        {
            foreach(var day in selectedDays)
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

        private async Task DeleteExistingScheduleData(int scheduleId)
        {
            await _timeRepository.DeleteScheduleTimesAsync(scheduleId);
            await _weekDaysRepository.DeleteScheduleWeekDaysAsync(scheduleId);
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
