using MedicinesTracker.Data;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IMedicineScheduleRepository
    {
        Task<MedicineScheduleDto?> GetMedicineScheduleById(int scheduleId);
        Task<int> AddMedicineSchedule(MedicationSchedule schedule);
        Task<int> UpdateMedicineScheduleAsync(MedicationSchedule schedule);
    }

    public class MedicineScheduleRepository : IMedicineScheduleRepository
    {
        private readonly AppDbContext _context;

        public MedicineScheduleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<MedicineScheduleDto?> GetMedicineScheduleById(int scheduleId)
        {
            return await _context.MedicationSchedules
                .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Unit)
                .Include(ms => ms.ScheduleType)
                .Include(ms => ms.ScheduleMode)
                .Include(ms => ms.RecurrencePattern)
                .Include(ms => ms.ScheduleWeekDays)
                    .ThenInclude(swd => swd.WeekDay)
                .Include(ms => ms.ScheduleTimes)
                .Where(ms => ms.IdSchedule == scheduleId)
                .Select(ms => new MedicineScheduleDto
                {
                    IdSchedule = ms.IdSchedule,
                    IdMedicine = ms.IdMedicine,
                    MedicineName = ms.Medicine.Name,
                    UnitName = ms.Medicine.Unit.Name,
                    IdScheduleType = ms.IdScheduleType,
                    ScheduleTypeCode = ms.ScheduleType.Code,
                    ScheduleTypeName = ms.ScheduleType.Name,
                    IdScheduleMode = ms.IdScheduleMode,
                    ScheduleModeCode = ms.ScheduleMode != null ? ms.ScheduleMode.Code : null,
                    ScheduleModeName = ms.ScheduleMode != null ? ms.ScheduleMode.Name : null,
                    IdRecurrencePattern = ms.IdRecurrencePattern,
                    RecurrencePatternName = ms.RecurrencePattern != null ? ms.RecurrencePattern.Name : null,
                    DaysInterval = ms.RecurrencePattern != null ? ms.RecurrencePattern.DaysInterval : null,
                    OneTimeDate = ms.OneTimeDate,
                    Dosage = ms.Dosage,
                    DateStart = ms.DateStart,
                    DateEnd = ms.DateEnd,
                    ScheduleIsActive = ms.IsActive,
                    WeekDayIds = string.Join(",", ms.ScheduleWeekDays.Select(swd => swd.IdDay)),
                    WeekDays = string.Join(", ", ms.ScheduleWeekDays.Select(swd => swd.WeekDay.Name)),
                    Times = string.Join(", ", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.Time)),
                    TimeOrders = string.Join(",", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.OrderInDay))
                })
                .FirstOrDefaultAsync();
        }

        public async Task<int> AddMedicineSchedule(MedicationSchedule schedule)
        {
            _context.MedicationSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            return schedule.IdSchedule;
        }

        public async Task<int> UpdateMedicineScheduleAsync(MedicationSchedule schedule)
        {
            var existing = await _context.MedicationSchedules.FindAsync(schedule.IdSchedule);
            if (existing == null) return 0;

            existing.IdMedicine = schedule.IdMedicine;
            existing.IdScheduleType = schedule.IdScheduleType;
            existing.IdScheduleMode = schedule.IdScheduleMode;
            existing.IdRecurrencePattern = schedule.IdRecurrencePattern;
            existing.OneTimeDate = schedule.OneTimeDate;
            existing.Dosage = schedule.Dosage;
            existing.DateStart = schedule.DateStart;
            existing.DateEnd = schedule.DateEnd;
            existing.IsActive = schedule.IsActive;

            return await _context.SaveChangesAsync();
        }
    }
}