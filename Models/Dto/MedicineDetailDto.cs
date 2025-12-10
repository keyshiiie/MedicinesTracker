using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models.Dto
{
    public class MedicineDetailDto
    {
        public int IdMedicine {  get; set; }
        public string? MedicineName { get; set; }
        public string? MethodAdmissionName { get; set; }
        public string? UnitName { get; set; }
        public string? Time { get; set; }                  
        public int IdReminder { get; set; }
        public string? DateEnd { get; set; }         
        public string? DateStart { get; set; }   
        public int Threshold { get; set; }
        public int Dosage { get; set; }
        public int CurrentQuantity { get; set; }
        public bool ReminderEnabled { get; set; }
        public string? FrequencyName { get; set; }
        public string? RecipientName { get; set; }
        public string? ScheduleString { get; set; }
    }

}
