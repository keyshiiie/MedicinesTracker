using Dapper;
using MedicinesTracker.Interface;
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
                r.Time,
                r.IdReminder,
                r.DateEnd,
                r.DateStart,
                r.Dosage,
                s.CurrentQuantity,
                s.Threshold,
                s.ReminderEnabled,
                f.Name AS FrequencyName,
                rec.Name AS RecipientName
            FROM Medicine m
            INNER JOIN Stock s ON m.IdMedicine = s.IdMedicine
            INNER JOIN Reminder r ON m.IdMedicine = r.IdMedicine
            INNER JOIN Recipient rec ON m.IdRecipient = rec.IdRecipient
            INNER JOIN MethodAdmission ma ON m.IdMethodAdmission = ma.IdMethodAdmission
            INNER JOIN Unit u ON m.IdUnit = u.IdUnit
            INNER JOIN Frequency f ON r.IdFrequency = f.IdFrequency
            ORDER BY m.Name, r.Time";

            
            return await _dbHandler.QueryAsync<MedicineDetailDto>(query);
        }

        public async Task<int> UpdateMedicineAsync(MedicineDto medicineDto)
        {
            if(!medicineDto.IdMedicine.HasValue) 
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
                medicineDto.IdMedicine,
                medicineDto.Name,
                medicineDto.IdUnit,
                medicineDto.IdMethodAdmission,
                medicineDto.IdRecipient
            };

            return await _dbHandler.ExecuteAsync(query, parameters);

        }
    }
}
