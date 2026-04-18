using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class Intake
    {
        [Key]
        public int IdIntake { get; set; }

        public int IdMedicine { get; set; }
        public bool IsCompleted { get; set; }
        public int IdSchedule { get; set; }
        public int IdScheduleTime { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? TakenDateTime { get; set; }
        public int ActualDosage { get; set; }

        // Навигационные свойства
        [ForeignKey(nameof(IdMedicine))]
        public virtual Medicine Medicine { get; set; }

        [ForeignKey(nameof(IdSchedule))]
        public virtual MedicationSchedule Schedule { get; set; }

        [ForeignKey(nameof(IdScheduleTime))]
        public virtual ScheduleTime ScheduleTime { get; set; }
    }
}