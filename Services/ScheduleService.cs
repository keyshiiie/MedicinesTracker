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
        private readonly INotificationSchedulerService _notificationService;
        private readonly IIntakeSchedulerService _intakeSchedulerService;

        public ScheduleService(
            IMedicineScheduleRepository scheduleRepository,
            IScheduleTimeRepository timeRepository,
            IScheduleWeekDaysRepository weekDaysRepository,
            INotificationSchedulerService notificationService,
            IIntakeSchedulerService intakeSchedulerService)
        {
            _scheduleRepository = scheduleRepository;
            _timeRepository = timeRepository;
            _weekDaysRepository = weekDaysRepository;
            _notificationService = notificationService;
            _intakeSchedulerService = intakeSchedulerService; 
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

            // 6. ВАЖНО: Перегенерируем записи приема для этого лекарства
            // Делаем в фоне, чтобы не блокировать UI
            _ = Task.Run(async () =>
            {
                try
                {
                    Debug.WriteLine($"=== Сохранение расписания для лекарства ID: {scheduleDto.IdMedicine} ===");

                    // 1. Перегенерируем записи приема
                    await _intakeSchedulerService.RegenerateIntakesForMedicineAsync(scheduleDto.IdMedicine);
                    Debug.WriteLine($"✅ Записи приема перегенерированы для лекарства ID: {scheduleDto.IdMedicine}");

                    // 2. Даем время на сохранение записей в БД
                    await Task.Delay(500);

                    // 3. Планируем уведомления
                    if (_notificationService != null)
                    {
                        // Отменяем старые уведомления
                        await CancelNotificationsForMedicineAsync(scheduleDto.IdMedicine);

                        // Планируем новые уведомления на сегодня
                        await _notificationService.ScheduleNotificationsForTodayAsync();

                        Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {scheduleDto.IdMedicine}");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ NotificationService не доступен");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка при перегенерации записей/уведомлений: {ex.Message}");
                }
            });

            return scheduleId;
        }

        private async Task CancelNotificationsForMedicineAsync(int medicineId)
        {
            try
            {
                if (_notificationService != null)
                {
                    // Используем новый метод, который вы добавили
                    await _notificationService.CancelNotificationsForMedicineAsync(medicineId);
                    Debug.WriteLine($"✅ Старые уведомления отменены для лекарства ID: {medicineId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка отмены уведомлений: {ex.Message}");
            }
        }

        public async Task ScheduleNotificationsForMedicineAsync(int medicineId)
        {
            try
            {
                Debug.WriteLine($"Планируем уведомления для лекарства ID: {medicineId}");

                if (_notificationService != null)
                {
                    await _notificationService.ScheduleAllNotificationsAsync();
                    Debug.WriteLine($"✅ Уведомления запланированы для лекарства ID: {medicineId}");
                }
                else
                {
                    Debug.WriteLine($"❌ NotificationService не инициализирован");
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

        private async Task DeleteExistingScheduleData(int scheduleId)
        {
            await _timeRepository.DeleteScheduleTimesAsync(scheduleId);
            await _weekDaysRepository.DeleteScheduleWeekDaysAsync(scheduleId);
        }
    }
}
