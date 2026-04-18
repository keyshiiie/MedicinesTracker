using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MedicinesTracker.Repository
{
    public interface IReferencesDataRepository
    {
        Task<IEnumerable<Unit>> GetAllUnitsAsync();
        Task<IEnumerable<MethodAdmission>> GetAllMethodsAdmissionAsync();
        Task<IEnumerable<ScheduleType>> GetAllScheduleTypeAsync();
        Task<IEnumerable<WeekDay>> GetAllWeekDayAsync();
        Task<IEnumerable<RecurrencePattern>> GetAllRecurrencePatternAsync();
        Task<IEnumerable<ScheduleMode>> GetAllScheduleModeAsync();
        Task ClearCacheAsync();
    }

    public class ReferencesDataRepository : IReferencesDataRepository
    {
        private readonly AppDbContext _context;

        // Опциональный кэш (можно оставить или убрать)
        private static List<Unit>? _cachedUnits;
        private static List<MethodAdmission>? _cachedMethods;
        private static List<ScheduleType>? _cachedScheduleTypes;
        private static List<WeekDay>? _cachedWeekDays;
        private static List<RecurrencePattern>? _cachedRecurrencePatterns;
        private static List<ScheduleMode>? _cachedScheduleModes;

        private static DateTime _lastCacheClear = DateTime.MinValue;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public ReferencesDataRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Unit>> GetAllUnitsAsync()
        {
            if (_cachedUnits != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedUnits;

            _cachedUnits = await _context.Units.OrderBy(u => u.Name).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedUnits;
        }

        public async Task<IEnumerable<MethodAdmission>> GetAllMethodsAdmissionAsync()
        {
            if (_cachedMethods != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedMethods;

            _cachedMethods = await _context.MethodAdmissions.OrderBy(m => m.Name).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedMethods;
        }

        public async Task<IEnumerable<ScheduleType>> GetAllScheduleTypeAsync()
        {
            if (_cachedScheduleTypes != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedScheduleTypes;

            _cachedScheduleTypes = await _context.ScheduleTypes.OrderBy(st => st.Name).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedScheduleTypes;
        }

        public async Task<IEnumerable<WeekDay>> GetAllWeekDayAsync()
        {
            if (_cachedWeekDays != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedWeekDays;

            _cachedWeekDays = await _context.WeekDays.OrderBy(w => w.Number).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedWeekDays;
        }

        public async Task<IEnumerable<RecurrencePattern>> GetAllRecurrencePatternAsync()
        {
            if (_cachedRecurrencePatterns != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedRecurrencePatterns;

            _cachedRecurrencePatterns = await _context.RecurrencePatterns.OrderBy(r => r.DaysInterval).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedRecurrencePatterns;
        }

        public async Task<IEnumerable<ScheduleMode>> GetAllScheduleModeAsync()
        {
            if (_cachedScheduleModes != null && DateTime.Now - _lastCacheClear < _cacheDuration)
                return _cachedScheduleModes;

            _cachedScheduleModes = await _context.ScheduleModes.OrderBy(sm => sm.Name).ToListAsync();
            _lastCacheClear = DateTime.Now;
            return _cachedScheduleModes;
        }

        public Task ClearCacheAsync()
        {
            _cachedUnits = null;
            _cachedMethods = null;
            _cachedScheduleTypes = null;
            _cachedWeekDays = null;
            _cachedRecurrencePatterns = null;
            _cachedScheduleModes = null;
            _lastCacheClear = DateTime.MinValue;

            Debug.WriteLine("References cache cleared");
            return Task.CompletedTask;
        }
    }
}