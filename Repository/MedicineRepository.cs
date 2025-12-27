using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using System.Diagnostics;

namespace MedicinesTracker.Repository
{
    public interface IMedicineRepository
    {
        Task<IEnumerable<MedicineDetailDto>> GetMedicineDetailsAsync();
        Task<int> UpdateMedicineAsync(MedicineModel medicineModel);
        Task<int> DeleteMedicineAsync(int idMedicine);
        Task<int> AddMedicineAsync(MedicineModel medicineModel);
        Task<MedicineDetailDto?> GetMedicineDetailByIdAsync(int idMedicine);
        Task<MedicineModel?> GetMedicineByIdAsync(int idMedicine);
    }
    public class MedicineRepository : IMedicineRepository
    {
        private readonly DBHandler _dbHandler;
        public MedicineRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task<IEnumerable<MedicineDetailDto>> GetMedicineDetailsAsync()
        {
            var query = @"
        SELECT
            m.IdMedicine,
            ms.IdSchedule,
            m.Name AS MedicineName,
            ma.Name AS MethodAdmissionName,
            u.Name AS UnitName,
            s.IdStock,
            s.CurrentQuantity,
            s.Threshold,
            CASE
                WHEN s.ReminderEnabled = 'true' OR s.ReminderEnabled = 1 THEN 1
                ELSE 0
            END AS ReminderEnabled,
            rec.Name AS RecipientName
        FROM Medicine m
        LEFT JOIN Stock s ON m.IdMedicine = s.IdMedicine
        LEFT JOIN MedicationSchedule ms ON m.IdMedicine = ms.IdMedicine
        INNER JOIN Recipient rec ON m.IdRecipient = rec.IdRecipient
        INNER JOIN MethodAdmission ma ON m.IdMethodAdmission = ma.IdMethodAdmission
        INNER JOIN Unit u ON m.IdUnit = u.IdUnit
        ORDER BY rec.Name";


            var result = await _dbHandler.QueryAsync<MedicineDetailDto>(query);
            Debug.WriteLine($"GetMedicineDetailsAsync вернул {result.Count()} записей");

            foreach (var item in result)
            {
                Debug.WriteLine($"  - {item.MedicineName} (ID: {item.IdMedicine}, Stock: {item.IdStock})");
            }

            return result;
        }

        public async Task<MedicineDetailDto?> GetMedicineDetailByIdAsync(int idMedicine)
        {
            var query = @"
            SELECT
                m.IdMedicine,
                m.Name AS MedicineName,
                ma.Name AS MethodAdmissionName,
                u.Name AS UnitName,
                s.IdStock,
                s.CurrentQuantity,
                s.Threshold,
                s.ReminderEnabled,
                rec.IdRecipient,
                rec.Name AS RecipientName
            FROM Medicine m
            INNER JOIN Stock s ON m.IdMedicine = s.IdMedicine
            INNER JOIN Recipient rec ON m.IdRecipient = rec.IdRecipient
            INNER JOIN MethodAdmission ma ON m.IdMethodAdmission = ma.IdMethodAdmission
            INNER JOIN Unit u ON m.IdUnit = u.IdUnit
            WHERE m.IdMedicine = @IdMedicine";

            var parameters = new { IdMedicine = idMedicine };

            return await _dbHandler.QueryFirstOrDefaultAsync<MedicineDetailDto>(query, parameters);
        }


        public async Task<MedicineModel?> GetMedicineByIdAsync(int idMedicine)
        {
            var query = @"
            SELECT 
                m.IdMedicine,
                m.Name,
                m.IdUnit,
                m.IdMethodAdmission,
                m.IdRecipient,
                m.CreatedAt,
                m.UpdatedAt
            FROM Medicine m
            WHERE m.IdMedicine = @IdMedicine";

            var parameters = new { IdMedicine = idMedicine };

            return await _dbHandler.QueryFirstOrDefaultAsync<MedicineModel>(query, parameters);
        }

        public async Task<int> UpdateMedicineAsync(MedicineModel medicineModel)
        {
            var query = @"
            UPDATE Medicine 
            SET 
                Name = @Name,
                IdUnit = @IdUnit,
                IdMethodAdmission = @IdMethodAdmission,
                IdRecipient = @IdRecipient
            WHERE IdMedicine = @IdMedicine";
            var parameters = new
            {
                medicineModel.IdMedicine,
                medicineModel.Name,
                medicineModel.IdUnit,
                medicineModel.IdMethodAdmission,
                medicineModel.IdRecipient
            };

            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<int> DeleteMedicineAsync(int idMedicine)
        {
            var query = @"DELETE FROM Medicine WHERE IdMedicine = @IdMedicine";
            var parameters = new
            {
                IdMedicine = idMedicine
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }

        public async Task<int> AddMedicineAsync(MedicineModel medicineModel)
        {
            var query = @"
        INSERT INTO Medicine (Name, IdUnit, IdMethodAdmission, IdRecipient)
        VALUES (@Name, @IdUnit, @IdMethodAdmission, @IdRecipient);
        SELECT LAST_INSERT_ROWID();";

            var parameters = new
            {
                medicineModel.Name,
                medicineModel.IdUnit,
                medicineModel.IdMethodAdmission,
                medicineModel.IdRecipient
            };

            // Используйте ExecuteScalarAsync для получения значения SELECT
            var newId = await _dbHandler.ExecuteScalarAsync<int>(query, parameters);
            return newId;
        }

    }
}
