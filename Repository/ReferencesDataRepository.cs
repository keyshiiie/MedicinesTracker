using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace MedicinesTracker.Repository
{
    public interface IReferencesDataRepository
    {
        Task<IEnumerable<UnitModel>> GetAllUnitsAsync();
        Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync();
        Task<IEnumerable<ScheduleTypeModel>> GetAllScheduleTypeAsync();
        Task<IEnumerable<WeekDayModel>> GetAllWeekDayAsync();
        Task<IEnumerable<RecurrencePatternModel>> GetAllRecurrencePatternAsync();
        Task<IEnumerable<ScheduleModeModel>> GetAllScheduleModeAsync();

        Task ClearCacheAsync();
    }

    public class ReferencesDataRepository : IReferencesDataRepository
    {
        private readonly IDBHandler _dbHandler;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        // Кэш для справочных данных (nullable)
        private static List<UnitModel>? _cachedUnits;
        private static List<MethodAdmissionModel>? _cachedMethods;
        private static List<ScheduleTypeModel>? _cachedScheduleTypes;
        private static List<WeekDayModel>? _cachedWeekDays;
        private static List<RecurrencePatternModel>? _cachedRecurrencePatterns;
        private static List<ScheduleModeModel>? _cachedScheduleModes;

        // Время последнего обновления кэша
        private static DateTime _unitsLastUpdate = DateTime.MinValue;
        private static DateTime _methodsLastUpdate = DateTime.MinValue;
        private static DateTime _scheduleTypesLastUpdate = DateTime.MinValue;
        private static DateTime _weekDaysLastUpdate = DateTime.MinValue;
        private static DateTime _recurrencePatternsLastUpdate = DateTime.MinValue;
        private static DateTime _scheduleModesLastUpdate = DateTime.MinValue;

        // Объекты для синхронизации
        private static readonly SemaphoreSlim _unitsLock = new(1, 1);
        private static readonly SemaphoreSlim _methodsLock = new(1, 1);
        private static readonly SemaphoreSlim _scheduleTypesLock = new(1, 1);
        private static readonly SemaphoreSlim _weekDaysLock = new(1, 1);
        private static readonly SemaphoreSlim _recurrencePatternsLock = new(1, 1);
        private static readonly SemaphoreSlim _scheduleModesLock = new(1, 1);

        public ReferencesDataRepository(IDBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task<IEnumerable<UnitModel>> GetAllUnitsAsync()
        {
            return await GetCachedDataAsync(
                _cachedUnits,
                _unitsLastUpdate,
                _unitsLock,
                () => _dbHandler.QueryAsync<UnitModel>("SELECT * FROM Unit ORDER BY Name"),
                data => _cachedUnits = data,
                time => _unitsLastUpdate = time);
        }

        public async Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync()
        {
            return await GetCachedDataAsync(
                _cachedMethods,
                _methodsLastUpdate,
                _methodsLock,
                () => _dbHandler.QueryAsync<MethodAdmissionModel>("SELECT * FROM MethodAdmission ORDER BY Name"),
                data => _cachedMethods = data,
                time => _methodsLastUpdate = time);
        }

        public async Task<IEnumerable<ScheduleTypeModel>> GetAllScheduleTypeAsync()
        {
            return await GetCachedDataAsync(
                _cachedScheduleTypes,
                _scheduleTypesLastUpdate,
                _scheduleTypesLock,
                () => _dbHandler.QueryAsync<ScheduleTypeModel>("SELECT * FROM ScheduleType ORDER BY Name"),
                data => _cachedScheduleTypes = data,
                time => _scheduleTypesLastUpdate = time);
        }

        public async Task<IEnumerable<WeekDayModel>> GetAllWeekDayAsync()
        {
            return await GetCachedDataAsync(
                _cachedWeekDays,
                _weekDaysLastUpdate,
                _weekDaysLock,
                () => _dbHandler.QueryAsync<WeekDayModel>("SELECT * FROM WeekDay ORDER BY Number"),
                data => _cachedWeekDays = data,
                time => _weekDaysLastUpdate = time);
        }

        public async Task<IEnumerable<RecurrencePatternModel>> GetAllRecurrencePatternAsync()
        {
            return await GetCachedDataAsync(
                _cachedRecurrencePatterns,
                _recurrencePatternsLastUpdate,
                _recurrencePatternsLock,
                () => _dbHandler.QueryAsync<RecurrencePatternModel>("SELECT * FROM RecurrencePattern ORDER BY DaysInterval"),
                data => _cachedRecurrencePatterns = data,
                time => _recurrencePatternsLastUpdate = time);
        }

        public async Task<IEnumerable<ScheduleModeModel>> GetAllScheduleModeAsync()
        {
            return await GetCachedDataAsync(
                _cachedScheduleModes,
                _scheduleModesLastUpdate,
                _scheduleModesLock,
                () => _dbHandler.QueryAsync<ScheduleModeModel>("SELECT * FROM ScheduleMode ORDER BY Name"),
                data => _cachedScheduleModes = data,
                time => _scheduleModesLastUpdate = time);
        }

        private async Task<List<T>> GetCachedDataAsync<T>(
            List<T>? cachedData,
            DateTime lastUpdate,
            SemaphoreSlim lockObject,
            Func<Task<IEnumerable<T>>> dataLoader,
            Action<List<T>> cacheSetter,
            Action<DateTime> timeSetter)
        {
            // Проверяем, нужно ли обновлять кэш
            if (cachedData != null && (DateTime.Now - lastUpdate) < _cacheDuration)
            {
                Debug.WriteLine($"Using cached data for {typeof(T).Name}");
                return cachedData;
            }

            // Блокируем для обновления кэша
            await lockObject.WaitAsync();
            try
            {
                // Двойная проверка (double-check)
                if (cachedData != null && (DateTime.Now - lastUpdate) < _cacheDuration)
                {
                    return cachedData;
                }

                Debug.WriteLine($"Loading fresh data for {typeof(T).Name}");

                // Загружаем данные из БД
                var data = await dataLoader();
                var result = data.ToList();

                // Обновляем кэш
                cacheSetter(result);
                timeSetter(DateTime.Now);

                Debug.WriteLine($"Cached {typeof(T).Name}: {result.Count} items");

                return result;
            }
            finally
            {
                lockObject.Release();
            }
        }

        public async Task ClearCacheAsync()
        {
            await Task.Run(() =>
            {
                _cachedUnits = null;
                _cachedMethods = null;
                _cachedScheduleTypes = null;
                _cachedWeekDays = null;
                _cachedRecurrencePatterns = null;
                _cachedScheduleModes = null;

                _unitsLastUpdate = DateTime.MinValue;
                _methodsLastUpdate = DateTime.MinValue;
                _scheduleTypesLastUpdate = DateTime.MinValue;
                _weekDaysLastUpdate = DateTime.MinValue;
                _recurrencePatternsLastUpdate = DateTime.MinValue;
                _scheduleModesLastUpdate = DateTime.MinValue;

                Debug.WriteLine("Cache cleared");
            });
        }
    }
}