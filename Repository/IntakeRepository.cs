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
        // CRUD
        Task<IEnumerable<HistoryDto>> GetAllIntakeAsync();
        Task<int> AddIntakeAsync(IntakeModel intakeModel);
        Task<int> UpdateIntakeAsync(IntakeModel intakeModel);
        Task<IntakeModel?> GetIntakeByIdAsync(int intakeId);

        // Поиск
        Task<IEnumerable<IntakeModel>> GetIntakesByDateAsync(string date);
        Task<IEnumerable<IntakeModel>> GetIntakesByMedicineAndDateAsync(int medicineId, string date);
        Task<IntakeModel?> GetIntakeByMedicineAndDateTimeAsync(int medicineId, string date, string time);
        Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync();

        // Проверка
        Task<bool> IntakeExistsAsync(int medicineId, string date, string time);

        // Удаление
        Task<int> DeleteFutureIntakesAsync(DateTime fromDate);
        Task<int> DeleteFutureIntakesForMedicineAsync(int medicineId, DateTime fromDate);
        Task<int> DeleteOldIntakesAsync(DateTime cutoffDate);
    }
    public class IntakeRepository : IIntakeRepository
    {
        private readonly IDBHandler _dbHandler;
        public IntakeRepository(IDBHandler bHandler)
        {
            _dbHandler = bHandler;
        }

        public async Task<IntakeModel?> GetIntakeByIdAsync(int intakeId)
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
        WHERE i.IdIntake = @IntakeId";

            return await _dbHandler.QueryFirstOrDefaultAsync<IntakeModel>(query, new { IntakeId = intakeId });
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
                      TakenDateTime = @TakenDateTime
                  WHERE IdIntake = @IdIntake";

            var parameters = new
            {
                intakeModel.IdIntake,
                intakeModel.IsCompleted,
                intakeModel.TakenDateTime
            };

            return await _dbHandler.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync()
        {
            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");

            // Показываем ВСЕ записи на сегодня (и принятые, и нет)
            // Но на странице "Сегодня" обычно показывают только не принятые
            var query = @"
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
            ms.IdSchedule,
            st.IdTime as IdScheduleTime,
            i.IdIntake,
            i.IsCompleted
        FROM Intake i
        JOIN Medicine m ON i.IdMedicine = m.IdMedicine
        JOIN MedicationSchedule ms ON i.IdSchedule = ms.IdSchedule
        JOIN ScheduleTime st ON i.IdScheduleTime = st.IdTime
        JOIN Unit u ON m.IdUnit = u.IdUnit
        JOIN Recipient r ON m.IdRecipient = r.IdRecipient
        JOIN Stock s ON m.IdMedicine = s.IdMedicine
        WHERE i.Date = @Today
          AND i.IsCompleted = 0  -- Только не принятые!
        ORDER BY r.Name, st.OrderInDay";

            return await _dbHandler.QueryAsync<TodayMedicineDto>(query, new { Today = today });
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

        public async Task<int> DeleteFutureIntakesAsync(DateTime fromDate)
        {
            var query = @"
            DELETE FROM Intake 
            WHERE Date >= @FromDate 
            AND IsCompleted = 0";

            var parameters = new { FromDate = fromDate.ToString("yyyy-MM-dd") };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<IntakeModel>> GetIntakesByMedicineAndDateAsync(int medicineId, string date)
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
    ORDER BY i.Time";

            var parameters = new { MedicineId = medicineId, Date = date };
            return await _dbHandler.QueryAsync<IntakeModel>(query, parameters);
        }

        public async Task<IntakeModel?> GetIntakeByMedicineAndDateTimeAsync(int medicineId, string date, string time)
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
      AND i.Time = @Time
    LIMIT 1";

            var parameters = new
            {
                MedicineId = medicineId,
                Date = date,
                Time = time // Теперь точно по времени
            };
            return await _dbHandler.QueryFirstOrDefaultAsync<IntakeModel>(query, parameters);
        }

        public async Task<bool> IntakeExistsAsync(int medicineId, string date, string time)
        {
            var query = @"
            SELECT COUNT(*) FROM Intake 
            WHERE IdMedicine = @MedicineId 
            AND Date = @Date 
            AND Time = @Time";

            var parameters = new { MedicineId = medicineId, Date = date, Time = time };
            var count = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return count > 0;
        }

        public async Task<int> DeleteFutureIntakesForMedicineAsync(int medicineId, DateTime fromDate)
        {
            var query = @"
            DELETE FROM Intake 
            WHERE IdMedicine = @MedicineId 
            AND Date >= @FromDate
            AND IsCompleted = 0";

            var parameters = new
            {
                MedicineId = medicineId,
                FromDate = fromDate.ToString("yyyy-MM-dd")
            };

            return await _dbHandler.ExecuteAsync(query, parameters);
        }

        public async Task<int> DeleteOldIntakesAsync(DateTime cutoffDate)
        {
            var query = @"
            DELETE FROM Intake 
            WHERE Date < @CutoffDate
            AND IsCompleted = 1";

            var parameters = new { CutoffDate = cutoffDate.ToString("yyyy-MM-dd") };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}