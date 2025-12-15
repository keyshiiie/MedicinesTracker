namespace MedicinesTracker.Models
{
    public class ReminderModel
    {
        public int IdReminder { get; set; }
        public int IdMedicine { get; set; }
        public int IdFrequency { get; set; }
        public string? FrequencyName { get; set; }

        public string? Time { get; set; }    
        public string? DateStart { get; set; }  
        public string? DateEnd { get; set; }  
        public int Dosage { get; set; }

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
