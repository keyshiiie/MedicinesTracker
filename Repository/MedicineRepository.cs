using MedicinesTracker.Interface;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;

namespace MedicinesTracker.Repository
{
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
                m.Name AS MedicineName,
                ma.Name AS MethodAdmissionName,
                u.Name AS UnitName,
                s.IdStock,
                s.InitialQuantity,
                s.CurrentQuantity,
                s.Threshold,
                s.ReminderEnabled,
                rec.Name AS RecipientName
            FROM Medicine m
            INNER JOIN Stock s ON m.IdMedicine = s.IdMedicine
            INNER JOIN Recipient rec ON m.IdRecipient = rec.IdRecipient
            INNER JOIN MethodAdmission ma ON m.IdMethodAdmission = ma.IdMethodAdmission
            INNER JOIN Unit u ON m.IdUnit = u.IdUnit
            ORDER BY rec.Name";
            
            return await _dbHandler.QueryAsync<MedicineDetailDto>(query);
        }

        public async Task<int> UpdateMedicineAsync(MedicineModel medicineModel)
        {
            if (!medicineModel.IdMedicine.HasValue)
                throw new ArgumentException("Для обновления требуется IdMedicine");
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
    }
}
