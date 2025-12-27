using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models
{
    public class WeekDayModel
    {
        public int IdDay { get; set; }
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
    }
}
