using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models
{
    public class MedicineScheduleModel
    {
        public int IdSchedule { get; set; }
        public int IdMedicine { get; set; }
        public int IdScheduleType { get; set; }
        public int? IdScheduleMode { get; set; }
        public int? IdRecurrencePattern { get; set; }
        public string? OneTimeDate { get; set; }
        public int Dosage { get; set; }
        public string DateStart { get; set; } = string.Empty;
        public string? DateEnd { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
