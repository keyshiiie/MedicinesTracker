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
    }
    public class ScheduleTimeRepository : IScheduleTimeRepository
    {
        private readonly DBHandler _dbHandler;
        public ScheduleTimeRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<int> AddScheduleTimeAsync(ScheduleTimeModel scheduleTimeModel)
        {
            var query = @"
            INSERT INTO ScheduleTime (
                IdTime,
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
                IdTime = @IdTime,
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
    }
}
