using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using System.Diagnostics;

namespace MedicinesTracker.Repository
{
    public interface IMedicineScheduleRepository
    {
        Task<MedicineScheduleDto?> GetMedicineScheduleById(int scheduleId);
        Task<int> AddMedicineShedule(MedicineScheduleModel scheduleModel);
        Task<int> UpdateMedicineScheduleAsync(MedicineScheduleModel scheduleModel);
    }
    public class MedicineScheduleRepository : IMedicineScheduleRepository
    {
        private readonly IDBHandler _dbHandler;
        public MedicineScheduleRepository(IDBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task<MedicineScheduleDto?> GetMedicineScheduleById(int scheduleId)
        {
            var query = @"
            SELECT
                ms.IdSchedule,
                ms.IdMedicine,
                m.Name AS MedicineName,
                u.Name AS UnitName,
        
                -- Тип расписания
                st.IdType AS IdScheduleType,
                st.Code AS ScheduleTypeCode,
                st.Name AS ScheduleTypeName,
        
                -- Режим расписания (только для RECURRING)
                sm.IdMode AS IdScheduleMode,
                sm.Code AS ScheduleModeCode,
                sm.Name AS ScheduleModeName,
        
                -- Периодичность (только для INTERVAL режима)
                rp.IdPattern AS IdRecurrencePattern,
                rp.Name AS RecurrencePatternName,
                rp.DaysInterval,
        
                -- Расписание
                ms.OneTimeDate,
                ms.Dosage,
                ms.DateStart,
                ms.DateEnd,
                ms.IsActive AS ScheduleIsActive,
        
                -- Дни недели для WEEKDAYS режима
                GROUP_CONCAT(wd.IdDay) AS WeekDayIds,
                GROUP_CONCAT(wd.Name, ', ') AS WeekDays,
        
                -- Время приёма в течение дня
                GROUP_CONCAT(stm.Time, ', ') AS Times,
        
                -- Порядок времени в течение дня (для сортировки)
                GROUP_CONCAT(stm.OrderInDay, ',') AS TimeOrders
            FROM MedicationSchedule ms
            JOIN Medicine m ON ms.IdMedicine = m.IdMedicine
            JOIN Unit u ON m.IdUnit = u.IdUnit
            JOIN ScheduleType st ON ms.IdScheduleType = st.IdType
    
            -- LEFT JOIN для ScheduleMode (может быть NULL для ONETIME)
            LEFT JOIN ScheduleMode sm ON ms.IdScheduleMode = sm.IdMode
    
            -- LEFT JOIN для RecurrencePattern (только для INTERVAL режима)
            LEFT JOIN RecurrencePattern rp ON ms.IdRecurrencePattern = rp.IdPattern
    
            -- LEFT JOIN для дней недели (только для WEEKDAYS режима)
            LEFT JOIN ScheduleWeekDays swd ON ms.IdSchedule = swd.IdSchedule
            LEFT JOIN WeekDay wd ON swd.IdDay = wd.IdDay
    
            -- LEFT JOIN для времени приема
            LEFT JOIN ScheduleTime stm ON ms.IdSchedule = stm.IdSchedule
            WHERE ms.IdSchedule = @IdSchedule
            GROUP BY
                ms.IdSchedule, ms.IdMedicine, m.Name, u.Name,
                st.Code, st.Name, 
                sm.Code, sm.Name, 
                rp.IdPattern, rp.Name, rp.DaysInterval,
                ms.OneTimeDate, ms.Dosage, ms.DateStart, 
                ms.DateEnd, ms.IsActive
            ORDER BY ms.DateStart DESC, ms.IdSchedule;
            ";

            var parameters = new { IdSchedule = scheduleId };
            return await _dbHandler.QueryFirstOrDefaultAsync<MedicineScheduleDto>(query, parameters);
        }

        public async Task<int> AddMedicineShedule(MedicineScheduleModel scheduleModel)
        {
            var query = @"
            INSERT INTO MedicationSchedule (
                IdMedicine,
                IdScheduleType,
                IdScheduleMode,
                IdRecurrencePattern,
                OneTimeDate,
                Dosage,
                DateStart,
                DateEnd,
                IsActive
            )
            VALUES (
                @IdMedicine,
                @IdScheduleType,
                @IdScheduleMode,
                @IdRecurrencePattern,
                @OneTimeDate,
                @Dosage,
                @DateStart,
                @DateEnd,
                @IsActive
            );
            SELECT LAST_INSERT_ROWID();";
            var parameters = new 
            { 
                scheduleModel.IdMedicine,
                scheduleModel.IdScheduleType,
                scheduleModel.IdScheduleMode,
                scheduleModel.IdRecurrencePattern,
                scheduleModel.OneTimeDate,
                scheduleModel.Dosage,
                scheduleModel.DateStart,
                scheduleModel.DateEnd,
                scheduleModel.IsActive
            };

            var newId = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return newId;
        }

        public async Task<int> UpdateMedicineScheduleAsync(MedicineScheduleModel scheduleModel)
        {
            var query = @"
            UPDATE MedicationSchedule
            SET
                IdMedicine = @IdMedicine,
                IdScheduleType = @IdScheduleType,
                IdScheduleMode = @IdScheduleMode,
                IdRecurrencePattern = @IdRecurrencePattern,
                OneTimeDate = @OneTimeDate,
                Dosage = @Dosage,
                DateStart = @DateStart,
                DateEnd = @DateEnd,
                IsActive = @IsActive
            WHERE IdSchedule = @IdSchedule;";
            var parameters = new
            {
                scheduleModel.IdSchedule,
                scheduleModel.IdMedicine,
                scheduleModel.IdScheduleType,
                scheduleModel.IdScheduleMode,
                scheduleModel.IdRecurrencePattern,
                scheduleModel.OneTimeDate,
                scheduleModel.Dosage,
                scheduleModel.DateStart,
                scheduleModel.DateEnd,
                scheduleModel.IsActive
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}
