using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models
{
    public class ScheduleTypeModel
    {
        public int IdType { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
