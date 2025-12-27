using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models
{
    public class ScheduleTimeModel
    {
        public int IdTime { get; set; }
        public int IdSchedule { get; set; }
        public string Time { get; set; } = string.Empty; // формат "08:00"
        public int OrderInDay { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
