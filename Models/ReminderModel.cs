namespace MedicinesTracker.Models
{
    public class ReminderModel
    {
        public int IdReminder { get; set; }
        public MedicineModel? Medicine { get; set; }
        public FrequencyModel? Frequency { get; set; }
        public string? Time { get; set; } 
        public string? DateStart { get; set; } 
        public string? DateEnd { get; set; } 
        public int Dosage { get; set; }
        public bool IsCompleted { get; set; }
        public UnitModel? Unit { get; set; } // Добавляем Unit
        public ReminderModel() { }

        public ReminderModel(int idReminder, MedicineModel medicine, FrequencyModel frequency, string time, string dateStart, string? dateEnd, int dosage, UnitModel? unit)
        {
            IdReminder = idReminder;
            Medicine = medicine;
            Frequency = frequency;
            Time = time;
            DateStart = dateStart;
            DateEnd = dateEnd;
            Dosage = dosage;
            Unit = unit;
        }

        public string DisplayDateEnd
        {
            get => string.IsNullOrWhiteSpace(DateEnd) ? "Дата окончания приёма не выбрана" : DateEnd;
        }
    }
}
