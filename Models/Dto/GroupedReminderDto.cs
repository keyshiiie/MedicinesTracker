using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models.Dto
{
    public class GroupedReminderDto
    {
        public string? FrequencyName { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public IEnumerable<ReminderDto>? Reminders { get; set; }
    }
}
