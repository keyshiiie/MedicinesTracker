using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models
{
    public class TimeGroup
    {
        public string? Time { get; set; }  // например, "08:00"
        public List<ReminderModel>? Reminders { get; set; }
    }

}
