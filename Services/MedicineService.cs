using MedicinesTracker.DataAccess;
using MedicinesTracker.Models;
using System.Data;

namespace MedicinesTracker.Services
{
    public class MedicineService
    {
        private readonly DBConnection _dbConnection;

        public MedicineService()
        {
            _dbConnection = new DBConnection();
        }

        public List<MedicineModel> GetAllMedicines()
        {
            var medicines = new Dictionary<int, MedicineModel>();

            string query = @"
                SELECT
                    m.IdMedicine,
                    m.Name AS MedicineName,
                    m.NameMethodAdmission,
                    m.NameUnit,
                    r.Time,
                    r.IdReminder,
                    r.DateEnd,
                    r.DateStart,
                    r.Dosage,
                    s.CurrentQuantity,
                    s.Threshold,
                    s.ReminderEnabled,
                    r.NameFrequency,
                    rec.Name AS Recipient
                FROM Medicine m
                INNER JOIN Stock s ON m.IdMedicine = s.IdMedicine
                INNER JOIN Reminder r ON m.IdMedicine = r.IdMedicine
                INNER JOIN Recipient rec ON m.IdRecipient = rec.IdRecipient
                ORDER BY m.Name, r.Time;";

            DataTable dataTable = _dbConnection.ExecuteReader(query);

            foreach (DataRow row in dataTable.Rows)
            {
                int idMedicine = Convert.ToInt32(row["IdMedicine"]);
                int idReminder = Convert.ToInt32(row["IdReminder"]);
                string medicineName = Convert.ToString(row["MedicineName"]) ?? string.Empty;
                string methodAdmissionName = Convert.ToString(row["NameMethodAdmission"]) ?? string.Empty;
                string unitName = Convert.ToString(row["NameUnit"]) ?? string.Empty;
                string time = Convert.ToString(row["Time"]) ?? string.Empty;
                string dateEndStr = Convert.ToString(row["DateEnd"]) ?? string.Empty;
                string dateStartStr = Convert.ToString(row["DateStart"]) ?? string.Empty;
                int dosage = Convert.ToInt32(row["Dosage"]);
                int currentQuantity = Convert.ToInt32(row["CurrentQuantity"]);
                int threshold = Convert.ToInt32(row["Threshold"]);
                bool reminderEnabled = Convert.ToBoolean(row["ReminderEnabled"]);
                string frequencyName = Convert.ToString(row["NameFrequency"]) ?? string.Empty;
                string recipientName = Convert.ToString(row["Recipient"]) ?? string.Empty;

                UnitModel unit = new UnitModel { Name = unitName };
                MethodAdmissionModel methodAdmission = new MethodAdmissionModel { Name = methodAdmissionName };
                RecipientModel recipient = new RecipientModel { Name = recipientName };
                FrequencyModel frequency = new FrequencyModel { Name = frequencyName };

                ReminderModel reminder = new ReminderModel
                {
                    IdReminder = idReminder,
                    Time = time,
                    DateStart = dateStartStr, 
                    DateEnd = dateEndStr,   
                    Dosage = dosage,
                    Frequency = frequency,
                    IsCompleted = false,
                    Unit = unit
                };

                StockModel stock = new StockModel
                {
                    CurrentQuantity = currentQuantity,
                    Threshold = threshold,
                    ReminderEnabled = reminderEnabled
                };

                if (!medicines.ContainsKey(idMedicine))
                {
                    medicines[idMedicine] = new MedicineModel(
                        idMedicine,
                        medicineName,
                        unit,
                        methodAdmission,
                        recipient,
                        stock,
                        new List<ReminderModel>() 
                    );
                }

                medicines[idMedicine].Reminders.Add(reminder);
            }

            return medicines.Values.ToList();
        }

    }
}
