using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Repository
{
    public interface IScheduleTimeRepository
    {
        Task<int> AddScheduleTimeAsync(ScheduleTimeModel scheduleTimeModel);
        Task<int> UpdateScheduleTimeAsync(ScheduleTimeModel scheduleTimeModel);
        Task<int> DeleteScheduleTimesAsync(int scheduleId);
    }
    public class ScheduleTimeRepository : IScheduleTimeRepository
    {
        private readonly IDBHandler _dbHandler;
        public ScheduleTimeRepository(IDBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<int> AddScheduleTimeAsync(ScheduleTimeModel scheduleTimeModel)
        {
            var query = @"
            INSERT INTO ScheduleTime (
                IdSchedule,
                Time,
                OrderInDay,
                IsActive
            )
            VALUES (
                @IdSchedule,
                @Time,
                @OrderInDay,
                @IsActive
            );
            SELECT LAST_INSERT_ROWID();";
            var parameters = new
            {
                scheduleTimeModel.IdSchedule,
                scheduleTimeModel.Time,
                scheduleTimeModel.OrderInDay,
                scheduleTimeModel.IsActive
            };
            var newId = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return newId;
        }
        public async Task<int> UpdateScheduleTimeAsync(ScheduleTimeModel scheduleTimeModel)
        {
            var query = @"
            UPDATE ScheduleTime
            SET 
                IdSchedule = @IdSchedule,
                Time = @Time,
                OrderInDay = @OrderInDay,
                IsActive = @IsActive
            WHERE IdTime = @IdTime";
            var parameters = new
            {
                scheduleTimeModel.IdTime,
                scheduleTimeModel.IdSchedule,
                scheduleTimeModel.Time,
                scheduleTimeModel.OrderInDay,
                scheduleTimeModel.IsActive
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<int> DeleteScheduleTimesAsync(int scheduleId)
        {
            var query = @"DELETE FROM ScheduleTime WHERE IdSchedule = @ScheduleId";
            var parameters = new { ScheduleId = scheduleId };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}
