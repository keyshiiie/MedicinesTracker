namespace MedicinesTracker.Models
{
    public class IntakeModel
    {
        public int IdIntake { get; set; }
        public int IdMedicine { get; set; }   
        public bool IsCompleted { get; set; } 
        public int IdSchedule { get; set; }
        public int IdScheduleTime { get; set; }
        public string? Date { get; set; }  
        public string? Time { get; set; }
        public int ActualDosage { get; set; }
        public string? Status { get; set; }
    }
}
