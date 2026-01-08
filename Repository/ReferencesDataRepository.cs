using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Repository
{
    public interface IReferencesDataRepository
    {
        Task<IEnumerable<UnitModel>> GetAllUnitsAsync();
        Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync();
        Task<IEnumerable<ScheduleTypeModel>> GetAllSheduleTypeAsync();
        Task<IEnumerable<WeekDayModel>> GetAllWeekDayAsync();
        Task<IEnumerable<RecurrencePatternModel>> GetAllRecurrencePatternAsync();
        Task<IEnumerable<ScheduleModeModel>> GetAllScheduleModeAsync();
    }
    public class ReferencesDataRepository : IReferencesDataRepository
    {
        private readonly IDBHandler _dbHandler;
        public ReferencesDataRepository(IDBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<IEnumerable<UnitModel>> GetAllUnitsAsync()
        {
            var query = @"SELECT * FROM Unit";
            return await _dbHandler.QueryAsync<UnitModel>(query);

        }
        public async Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync()
        {
            var query = @"SELECT * FROM MethodAdmission";
            return await _dbHandler.QueryAsync<MethodAdmissionModel>(query);
        }
        public async Task<IEnumerable<ScheduleTypeModel>> GetAllSheduleTypeAsync()
        {
            var query = @"SELECT * FROM ScheduleType";
            return await _dbHandler.QueryAsync<ScheduleTypeModel>(query);
        }
        public async Task<IEnumerable<WeekDayModel>> GetAllWeekDayAsync()
        {
            var query = @"SELECT * FROM WeekDay";
            return await _dbHandler.QueryAsync<WeekDayModel>(query);
        }
        public async Task<IEnumerable<RecurrencePatternModel>> GetAllRecurrencePatternAsync()
        {
            var query = @"SELECT * FROM RecurrencePattern";
            return await _dbHandler.QueryAsync<RecurrencePatternModel>(query);
        }
        public async Task<IEnumerable<ScheduleModeModel>> GetAllScheduleModeAsync()
        {
            var query = @"SELECT * FROM ScheduleMode";
            return await _dbHandler.QueryAsync<ScheduleModeModel>(query);
        }
    }
}
