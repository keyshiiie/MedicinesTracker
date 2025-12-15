using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models.Dto
{
    public class ReminderDto
    {
        public int IdReminder { get; set; }
        public string? Time { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public int Dosage { get; set; }
        public int IdFrequency { get; set; }
        public string? FrequencyName { get; set; }
    }
}
