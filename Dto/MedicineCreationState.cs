using MedicinesTracker.Entities;

namespace MedicinesTracker.Dto
{
    public class MedicineCreationState
    {
        public Medicine? Medicine { get; set; }
        public Stock? Stock { get; set; }
        public MedicineScheduleDto? Schedule { get; set; }
        public List<WeekDay>? SelectedDays { get; set; }
        public List<TimeSpan>? SelectedTimes { get; set; }
        public bool IsComplete { get; set; }
    }
}
