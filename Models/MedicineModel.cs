namespace MedicinesTracker.Models
{
    public class MedicineModel
    {
        public int IdMedicine { get; set; }
        public string? Name { get; set; }
        public UnitModel? Unit { get; set; }
        public MethodAdmissionModel? MethodAdmission { get; set; }
        public RecipientModel? Recipient { get; set; }
        public StockModel? Stock { get; set; }
        public List<ReminderModel>? Reminders { get; set; } 

        public MedicineModel() { }

        public MedicineModel(int idMedicine, string name, UnitModel unit, MethodAdmissionModel methodAdmission, RecipientModel recipient, StockModel stock, List<ReminderModel> reminders)
        {
            IdMedicine = idMedicine;
            Name = name;
            Unit = unit;
            MethodAdmission = methodAdmission;
            Recipient = recipient;
            Stock = stock;
            Reminders = reminders;
        }

        public string ScheduleString
        {
            get
            {
                if (Reminders == null || Reminders.Count == 0)
                    return string.Empty;

                var times = Reminders.Select(r => r.Time).ToList();
                var frequencyText = $"{Reminders.Count} раза в день";

                if (times.Count == 1)
                    return $"{frequencyText} — {times[0]}";
                else if (times.Count == 2)
                    return $"{frequencyText} — {times[0]} и {times[1]}";
                else
                    return $"{frequencyText} — {string.Join(", ", times.Take(times.Count - 1))} и {times.Last()}";
            }
        }
    }
}
