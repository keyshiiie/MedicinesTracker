using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Dto
{
    public class TodayMedicineDto
    {
        public int IdMedicine { get; set; }
        public int IdStock { get; set; }
        public int CurrentQuantity { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string MedicineName {  get; set; } = string.Empty;
        public int Dosage { get; set; }
        public int IdScheduleTime { get; set; }
        public string Time { get; set; } = string.Empty;
        public int OrderInDay { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public int IdSchedule { get; set; }
        public string ScheduleType { get; set; } = string.Empty;
        public int IdIntake { get; set; }
        public bool IsCompleted { get; set; }
    }
}
