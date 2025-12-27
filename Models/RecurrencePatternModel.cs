namespace MedicinesTracker.Models
{
    public class RecurrencePatternModel
    {
        public int IdPattern { get; set; }
        public string? Name { get; set; }
        public int DaysInterval { get; set; }
        public string? Description { get; set; }
    }
}
