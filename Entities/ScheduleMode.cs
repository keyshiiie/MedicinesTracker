using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public class ScheduleMode
    {
        [Key]
        public int IdMode { get; set; }

        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        // Обратная навигация
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
    }
}