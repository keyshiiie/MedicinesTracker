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
        public required string Time { get; set; }
        public string? DateEnd { get; set; }
        public required string DateStart { get; set; }
        public int Dosage { get; set; }
        public required string FrequencyName { get; set; }
    }
}
