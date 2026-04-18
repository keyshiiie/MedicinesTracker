using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public class RecurrencePattern
    {
        [Key]
        public int IdPattern { get; set; }

        public string? Name { get; set; }
        public int DaysInterval { get; set; }
        public string? Description { get; set; }

        // Обратная навигация
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
    }
}