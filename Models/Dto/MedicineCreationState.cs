using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models.Dto
{
    public class MedicineCreationState
    {
        public MedicineModel? Medicine { get; set; }
        public StockModel? Stock { get; set; }
        public MedicineScheduleDto? Schedule { get; set; }
        public List<WeekDayModel>? SelectedDays { get; set; }
        public List<TimeSpan>? SelectedTimes { get; set; }
        public bool IsComplete { get; set; }
    }
}
