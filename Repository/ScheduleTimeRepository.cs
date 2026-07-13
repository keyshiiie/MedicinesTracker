using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IScheduleTimeRepository
    {
        Task<int> AddScheduleTimeAsync(ScheduleTime scheduleTime);
        Task<int> UpdateScheduleTimeAsync(ScheduleTime scheduleTime);
        Task<int> DeleteScheduleTimesAsync(int scheduleId);
        Task<int?> GetScheduleTimeIdByTimeAsync(int scheduleId, string time);
        Task<IEnumerable<ScheduleTime>> GetScheduleTimesByScheduleIdAsync(int scheduleId);
        Task<ScheduleTime?> GetScheduleTimeByScheduleAndTimeAsync(int scheduleId, string time);
    }

    public class ScheduleTimeRepository : IScheduleTimeRepository
    {
        private readonly AppDbContext _context;

        public ScheduleTimeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddScheduleTimeAsync(ScheduleTime scheduleTime)
        {
            _context.ScheduleTimes.Add(scheduleTime);
            await _context.SaveChangesAsync();
            return scheduleTime.IdTime;
        }

        public async Task<int> UpdateScheduleTimeAsync(ScheduleTime scheduleTime)
        {
            var existing = await _context.ScheduleTimes.FindAsync(scheduleTime.IdTime);
            if (existing == null) return 0;

            existing.IdSchedule = scheduleTime.IdSchedule;
            existing.Time = scheduleTime.Time;
            existing.OrderInDay = scheduleTime.OrderInDay;
            existing.IsActive = scheduleTime.IsActive;

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteScheduleTimesAsync(int scheduleId)
        {
            var times = await _context.ScheduleTimes
                .Where(st => st.IdSchedule == scheduleId)
                .ToListAsync();

            _context.ScheduleTimes.RemoveRange(times);
            return await _context.SaveChangesAsync();
        }

        public async Task<int?> GetScheduleTimeIdByTimeAsync(int scheduleId, string time)
        {
            return await _context.ScheduleTimes
                .Where(st => st.IdSchedule == scheduleId && st.Time == time)
                .Select(st => (int?)st.IdTime)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ScheduleTime>> GetScheduleTimesByScheduleIdAsync(int scheduleId)
        {
            return await _context.ScheduleTimes
                .Where(st => st.IdSchedule == scheduleId && st.IsActive)
                .OrderBy(st => st.OrderInDay)
                .ToListAsync();
        }

        public async Task<ScheduleTime?> GetScheduleTimeByScheduleAndTimeAsync(int scheduleId, string time)
        {
            return await _context.ScheduleTimes
                .FirstOrDefaultAsync(st => st.IdSchedule == scheduleId && st.Time == time && st.IsActive);
        }
    }
}