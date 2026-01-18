using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using Microsoft.Data.Sqlite;
using Syncfusion.Maui.Toolkit.Carousel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
namespace MedicinesTracker.Repository
{
    public interface IIntakeRepository
    {
        Task<IEnumerable<HistoryDto>> GetAllIntakeAsync();
        Task<int> AddIntakeAsync(IntakeModel intakeModel);
        Task<int> UpdateIntakeAsync(IntakeModel intakeModel);
        Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync();
        Task<IntakeModel?> GetIntakeByMedicineAndDateAsync(int medicineId, string date);
        Task<IEnumerable<IntakeModel>> GetIntakesByDateAsync(string date);
        Task<int> DeleteFutureIntakesAsync(DateTime fromDate);
        Task<bool> HasIntakesForDateAsync(string date);
    }
    public class IntakeRepository : IIntakeRepository
    {
        private readonly IDBHandler _dbHandler;
        public IntakeRepository(IDBHandler bHandler)
        {
            _dbHandler = bHandler;
        }

        public async Task<IEnumerable<HistoryDto>> GetAllIntakeAsync()
        {
            var query = @"
    SELECT
        i.IdIntake,
        m.IdMedicine,
        m.Name AS NameMedicine,
        i.IsCompleted,
        i.IdSchedule,
        i.IdScheduleTime,
        st.OrderInDay,
        i.Date,
        i.Time,
        i.TakenDateTime,
        i.ActualDosage,
        m.IdUnit,
        r.Name AS RecipientName,
        u.Name AS UnitName,
        r.IdRecipient
    FROM Intake i 
    JOIN Medicine m ON i.IdMedicine = m.IdMedicine
    JOIN Unit u ON m.IdUnit = u.IdUnit
    JOIN Recipient r ON m.IdRecipient = r.IdRecipient
    LEFT JOIN ScheduleTime st ON st.IdTime = i.IdScheduleTime  -- Важно: IdTime = IdScheduleTime
    ORDER BY i.Date DESC, i.Time DESC";

            return await _dbHandler.QueryAsync<HistoryDto>(query);
        }

        public async Task<int> AddIntakeAsync(IntakeModel intakeModel)
        {
            var query = @"INSERT INTO Intake 
            (IdMedicine, IsCompleted, IdSchedule, IdScheduleTime, Date, Time, TakenDateTime, ActualDosage)
            VALUES (@IdMedicine, @IsCompleted, @IdSchedule, @IdScheduleTime, @Date, @Time, @TakenDateTime, @ActualDosage);
            SELECT LAST_INSERT_ROWID();";

            var parameters = new
            {
                intakeModel.IdMedicine,
                intakeModel.IsCompleted,
                intakeModel.IdSchedule,
                intakeModel.IdScheduleTime,
                Date = intakeModel.Date, // Уже в формате yyyy-MM-dd
                TakenDateTime = intakeModel.TakenDateTime,
                intakeModel.Time,
                intakeModel.ActualDosage
            };
            var newId = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return newId;
        }
        public async Task<int> UpdateIntakeAsync(IntakeModel intakeModel)
        {
            var query = @"UPDATE Intake 
    SET IsCompleted = @IsCompleted,
        Time = @Time,
        TakenDateTime = @TakenDateTime,
        ActualDosage = @ActualDosage  
    WHERE IdIntake = @IdIntake";

            var parameters = new
            {
                intakeModel.IdIntake,
                intakeModel.IsCompleted,
                Time = intakeModel.Time,
                TakenDateTime = intakeModel.TakenDateTime,
                ActualDosage = intakeModel.ActualDosage  // Добавьте если есть
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync()
        {
            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");

            // Запрос для получения лекарств на сегодня, которые ЕЩЕ НЕ ПРИНЯТЫ
            var query = @"
            WITH today_date AS (SELECT date('now') AS today),
                 current_weekday AS (
                     SELECT CASE 
                                WHEN strftime('%w', date('now')) = '0' THEN 7
                                ELSE CAST(strftime('%w', date('now')) AS INTEGER)
                            END AS weekday_number
                 )
            -- Одноразовые расписания (НЕ ПРИНЯТЫЕ)
            SELECT 
                m.IdMedicine,
                s.IdStock,
                s.CurrentQuantity,
                r.Name AS RecipientName,
                m.Name as MedicineName,
                ms.Dosage,
                st.Time,
                st.OrderInDay,
                u.Name as UnitName,
                'ONETIME' as ScheduleType,
                ms.IdSchedule,
                st.IdTime as IdScheduleTime,
                -- Проверяем, есть ли уже запись в Intake
                COALESCE(i.IdIntake, 0) as IdIntake,
                -- Извлекаем статус приема
                COALESCE(i.IsCompleted, 0) as IsCompleted,
                -- Извлекаем фактическую дозировку если есть
                COALESCE(i.ActualDosage, ms.Dosage) as ActualDosage
            FROM MedicationSchedule ms
            JOIN Medicine m ON ms.IdMedicine = m.IdMedicine
            JOIN ScheduleTime st ON ms.IdSchedule = st.IdSchedule
            JOIN Unit u ON m.IdUnit = u.IdUnit
            JOIN Recipient r ON m.IdRecipient = r.IdRecipient
            JOIN Stock s ON m.IdMedicine = s.IdMedicine
            LEFT JOIN Intake i ON i.IdMedicine = m.IdMedicine 
                AND i.IdSchedule = ms.IdSchedule
                AND i.IdScheduleTime = st.IdTime
                AND i.Date = date('now')
            WHERE ms.IsActive = 1
              AND st.IsActive = 1
              AND ms.IdScheduleType = (SELECT IdType FROM ScheduleType WHERE Code = 'ONETIME')
              AND ms.OneTimeDate = date('now')
              -- Фильтр: только не принятые лекарства
              AND (i.IdIntake IS NULL OR i.IsCompleted = 0)

            UNION ALL

            -- Интервальные расписания (НЕ ПРИНЯТЫЕ)
            SELECT 
                m.IdMedicine,
                s.IdStock,
                s.CurrentQuantity,
                r.Name as RecipientName,
                m.Name as MedicineName,
                ms.Dosage,
                st.Time,
                st.OrderInDay,
                u.Name as UnitName,
                'INTERVAL' as ScheduleType,
                ms.IdSchedule,
                st.IdTime as IdScheduleTime,
                COALESCE(i.IdIntake, 0) as IdIntake,
                COALESCE(i.IsCompleted, 0) as IsCompleted,
                COALESCE(i.ActualDosage, ms.Dosage) as ActualDosage
            FROM MedicationSchedule ms
            JOIN Medicine m ON ms.IdMedicine = m.IdMedicine
            JOIN ScheduleTime st ON ms.IdSchedule = st.IdSchedule
            JOIN RecurrencePattern rp ON ms.IdRecurrencePattern = rp.IdPattern
            JOIN Unit u ON m.IdUnit = u.IdUnit
            JOIN Recipient r ON m.IdRecipient = r.IdRecipient
            JOIN Stock s ON m.IdMedicine = s.IdMedicine
            LEFT JOIN Intake i ON i.IdMedicine = m.IdMedicine 
                AND i.IdSchedule = ms.IdSchedule
                AND i.IdScheduleTime = st.IdTime
                AND i.Date = date('now')
            WHERE ms.IsActive = 1
              AND st.IsActive = 1
              AND ms.IdScheduleType = (SELECT IdType FROM ScheduleType WHERE Code = 'RECURRING')
              AND ms.IdScheduleMode = (SELECT IdMode FROM ScheduleMode WHERE Code = 'INTERVAL')
              AND (ms.DateStart IS NULL OR ms.DateStart <= date('now'))
              AND (ms.DateEnd IS NULL OR ms.DateEnd >= date('now'))
              AND (
                  (ms.DateStart IS NOT NULL AND 
                   (julianday(date('now')) - julianday(ms.DateStart)) % rp.DaysInterval = 0)
                  OR
                  (ms.OneTimeDate IS NOT NULL AND 
                   (julianday(date('now')) - julianday(ms.OneTimeDate)) % rp.DaysInterval = 0)
              )
              -- Фильтр: только не принятые лекарства
              AND (i.IdIntake IS NULL OR i.IsCompleted = 0)

            UNION ALL

            -- Расписания по дням недели (НЕ ПРИНЯТЫЕ)
            SELECT 
                m.IdMedicine,
                s.IdStock,
                s.CurrentQuantity,
                r.Name as RecipientName,
                m.Name as MedicineName,
                ms.Dosage,
                st.Time,
                st.OrderInDay,
                u.Name as UnitName,
                'WEEKDAYS' as ScheduleType,
                ms.IdSchedule,
                st.IdTime as IdScheduleTime,
                COALESCE(i.IdIntake, 0) as IdIntake,
                COALESCE(i.IsCompleted, 0) as IsCompleted,
                COALESCE(i.ActualDosage, ms.Dosage) as ActualDosage
            FROM MedicationSchedule ms
            JOIN Medicine m ON ms.IdMedicine = m.IdMedicine
            JOIN ScheduleTime st ON ms.IdSchedule = st.IdSchedule
            JOIN ScheduleWeekDays swd ON ms.IdSchedule = swd.IdSchedule
            JOIN Unit u ON m.IdUnit = u.IdUnit
            JOIN Recipient r ON m.IdRecipient = r.IdRecipient
            JOIN Stock s ON m.IdMedicine = s.IdMedicine
            LEFT JOIN Intake i ON i.IdMedicine = m.IdMedicine 
                AND i.IdSchedule = ms.IdSchedule
                AND i.IdScheduleTime = st.IdTime
                AND i.Date = date('now')
            WHERE ms.IsActive = 1
              AND st.IsActive = 1
              AND ms.IdScheduleType = (SELECT IdType FROM ScheduleType WHERE Code = 'RECURRING')
              AND ms.IdScheduleMode = (SELECT IdMode FROM ScheduleMode WHERE Code = 'WEEKDAYS')
              AND (ms.DateStart IS NULL OR ms.DateStart <= date('now'))
              AND (ms.DateEnd IS NULL OR ms.DateEnd >= date('now'))
              AND swd.IdDay = (
                  SELECT CASE 
                             WHEN strftime('%w', date('now')) = '0' THEN 7
                             ELSE CAST(strftime('%w', date('now')) AS INTEGER)
                         END
              )
              -- Фильтр: только не принятые лекарства
              AND (i.IdIntake IS NULL OR i.IsCompleted = 0)

            ORDER BY 6, 5, 2;";

            var todayMedicines = await _dbHandler.QueryAsync<TodayMedicineDto>(query);

            // Создаем записи в Intake для тех лекарств, у которых их еще нет
            var medicinesWithoutIntake = todayMedicines
                .Where(m => m.IdIntake == 0)
                .ToList();

            if (medicinesWithoutIntake.Any())
            {
                foreach (var medicine in medicinesWithoutIntake)
                {
                    var intakeModel = new IntakeModel
                    {
                        IdMedicine = medicine.IdMedicine,
                        IdSchedule = medicine.IdSchedule,
                        IdScheduleTime = medicine.IdScheduleTime,
                        IsCompleted = false,
                        Date = DateTime.Now.Date.ToString("yyyy-MM-dd"),
                        Time = medicine.Time,
                        ActualDosage = medicine.Dosage
                    };

                    var newId = await AddIntakeAsync(intakeModel);
                    // Обновляем IdIntake в коллекции
                    medicine.IdIntake = newId;
                    // Поскольку запись только создана, IsCompleted = false
                    medicine.IsCompleted = false;
                }
            }

            return todayMedicines;
        }

        public async Task<IntakeModel?> GetIntakeByMedicineAndDateAsync(int medicineId, string date)
        {
            var query = @"
        SELECT 
            i.IdIntake,
            i.IdMedicine,
            i.IsCompleted,
            i.IdSchedule,
            i.IdScheduleTime,
            i.Date,
            i.Time,
            i.TakenDateTime,
            i.ActualDosage
        FROM Intake i
        WHERE i.IdMedicine = @MedicineId 
          AND i.Date = @Date
        LIMIT 1";

            var parameters = new { MedicineId = medicineId, Date = date };
            return await _dbHandler.QueryFirstOrDefaultAsync<IntakeModel>(query, parameters);
        }

        public async Task<IEnumerable<IntakeModel>> GetIntakesByDateAsync(string date)
        {
            var query = @"
        SELECT 
            i.IdIntake,
            i.IdMedicine,
            i.IsCompleted,
            i.IdSchedule,
            i.IdScheduleTime,
            i.Date,
            i.Time,
            i.TakenDateTime,
            i.ActualDosage
        FROM Intake i
        WHERE i.Date = @Date
        ORDER BY i.Time";

            var parameters = new { Date = date };
            return await _dbHandler.QueryAsync<IntakeModel>(query, parameters);
        }

        public async Task<bool> HasIntakesForDateAsync(string date)
        {
            var query = "SELECT COUNT(*) FROM Intake WHERE Date = @Date";
            var parameters = new { Date = date };
            var count = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return count > 0;
        }

        public async Task<int> DeleteFutureIntakesAsync(DateTime fromDate)
        {
            var query = @"
            DELETE FROM Intake 
            WHERE Date >= @FromDate 
            AND IsCompleted = 0";

            var parameters = new { FromDate = fromDate.ToString("yyyy-MM-dd") };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}