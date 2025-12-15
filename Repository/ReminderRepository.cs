using MedicinesTracker.Interface;
using MedicinesTracker.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Repository
{
    public class ReminderRepository : IReminderRepository
    {
        private readonly DBHandler _dbHandler;
        public ReminderRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<IEnumerable<GroupedReminderDto>> GetGroupedRemindersByMedicineIdAsync(int medicineId)
        {
            var query = @"
        SELECT
            r.IdReminder,
            r.IdMedicine,
            f.IdFrequency,
            f.Name AS FrequencyName,
            r.Time,
            r.DateStart,
            r.DateEnd,
            r.Dosage
        FROM Reminder r
        INNER JOIN Frequency f ON r.IdFrequency = f.IdFrequency
        WHERE r.IdMedicine = @IdMedicine";

            var parameters = new { IdMedicine = medicineId };
            var reminders = await _dbHandler.QueryAsync<ReminderDto>(query, parameters);

            // Группируем по FrequencyName
            var grouped = reminders
            .GroupBy(r => new
            {
                r.FrequencyName,
                r.DateStart,
                r.DateEnd
            })
            .Select(g => new GroupedReminderDto
            {
                FrequencyName = g.Key.FrequencyName,
                DateStart = g.Key.DateStart,
                DateEnd = g.Key.DateEnd,
                Reminders = g.OrderBy(r => r.Time).ToList()
            })
            .ToList();

            return grouped;
        }


    }
}
