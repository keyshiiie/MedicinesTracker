using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class ScheduleTime
    {
        [Key]
        public int IdTime { get; set; }

        public int IdSchedule { get; set; }
        public string Time { get; set; } = string.Empty;
        public int OrderInDay { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        [ForeignKey(nameof(IdSchedule))]
        public virtual MedicationSchedule Schedule { get; set; } = null!;

        // Обратная навигация
        public virtual ICollection<Intake> Intakes { get; set; } = new List<Intake>();
    }
}