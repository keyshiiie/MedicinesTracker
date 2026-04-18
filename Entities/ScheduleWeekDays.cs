using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class ScheduleWeekDay
    {
        [Key]
        public int IdLink { get; set; }

        public int IdSchedule { get; set; }
        public int IdDay { get; set; }

        // Навигационные свойства
        [ForeignKey(nameof(IdSchedule))]
        public virtual MedicationSchedule Schedule { get; set; }

        [ForeignKey(nameof(IdDay))]
        public virtual WeekDay WeekDay { get; set; }
    }
}