using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IScheduleWeekDaysRepository
    {
        Task<int> AddScheduleWeekDayAsync(ScheduleWeekDay scheduleWeekDay);
        Task<int> UpdateScheduleWeekDayAsync(ScheduleWeekDay scheduleWeekDay);
        Task<int> DeleteScheduleWeekDaysAsync(int scheduleId);
    }

    public class ScheduleWeekDaysRepository : IScheduleWeekDaysRepository
    {
        private readonly AppDbContext _context;

        public ScheduleWeekDaysRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddScheduleWeekDayAsync(ScheduleWeekDay scheduleWeekDay)
        {
            _context.ScheduleWeekDays.Add(scheduleWeekDay);
            await _context.SaveChangesAsync();
            return scheduleWeekDay.IdLink;
        }

        public async Task<int> UpdateScheduleWeekDayAsync(ScheduleWeekDay scheduleWeekDay)
        {
            var existing = await _context.ScheduleWeekDays.FindAsync(scheduleWeekDay.IdLink);
            if (existing == null) return 0;

            existing.IdSchedule = scheduleWeekDay.IdSchedule;
            existing.IdDay = scheduleWeekDay.IdDay;

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteScheduleWeekDaysAsync(int scheduleId)
        {
            var weekDays = await _context.ScheduleWeekDays
                .Where(swd => swd.IdSchedule == scheduleId)
                .ToListAsync();

            _context.ScheduleWeekDays.RemoveRange(weekDays);
            return await _context.SaveChangesAsync();
        }
    }
}