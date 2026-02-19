using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Repository
{
    public interface IScheduleWeekDaysRepository
    {
        Task<int> AddScheduleWeekDayAsync(ScheduleWeekDaysModel scheduleWeekDaysModel);
        Task<int> UpdateScheduleWeekDayAsync(ScheduleWeekDaysModel scheduleWeekDaysModel);
        Task<int> DeleteScheduleWeekDaysAsync(int scheduleId);
    }
    public class ScheduleWeekDaysRepository : IScheduleWeekDaysRepository
    {
        private readonly IDBHandler _dbHandler;
        public ScheduleWeekDaysRepository(IDBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<int> AddScheduleWeekDayAsync(ScheduleWeekDaysModel scheduleWeekDaysModel)
        {
            var query = @"
            INSERT INTO ScheduleWeekDays (IdSchedule, IdDay)
            VALUES (@IdSchedule,@IdDay);
            SELECT LAST_INSERT_ROWID();";
            var parameters = new
            {
                scheduleWeekDaysModel.IdSchedule,
                scheduleWeekDaysModel.IdDay
            };
            var newId = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return newId;
        }
        public async Task<int> UpdateScheduleWeekDayAsync(ScheduleWeekDaysModel scheduleWeekDaysModel)
        {
            var query = @"UPDATE ScheduleWeekDays
            SET IdSchedule = @IdSchedule,
                IdDay = IdDay
            WHERE IdLink = @IdLink";
            var parameters = new
            {
                scheduleWeekDaysModel.IdLink,
                scheduleWeekDaysModel.IdSchedule,
                scheduleWeekDaysModel.IdDay
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<int> DeleteScheduleWeekDaysAsync(int scheduleId)
        {
            var query = @"DELETE FROM ScheduleWeekDays WHERE IdSchedule = @ScheduleId";
            var parameters = new { ScheduleId = scheduleId };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}
