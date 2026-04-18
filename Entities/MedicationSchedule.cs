using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class MedicationSchedule
    {
        [Key]
        public int IdSchedule { get; set; }

        public int IdMedicine { get; set; }
        public int IdScheduleType { get; set; }
        public int? IdScheduleMode { get; set; }
        public int? IdRecurrencePattern { get; set; }
        public string? OneTimeDate { get; set; }
        public int Dosage { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        [ForeignKey(nameof(IdMedicine))]
        public virtual Medicine Medicine { get; set; }

        [ForeignKey(nameof(IdScheduleType))]
        public virtual ScheduleType ScheduleType { get; set; }

        [ForeignKey(nameof(IdScheduleMode))]
        public virtual ScheduleMode ScheduleMode { get; set; }

        [ForeignKey(nameof(IdRecurrencePattern))]
        public virtual RecurrencePattern RecurrencePattern { get; set; }

        // Коллекции для обратных связей
        public virtual ICollection<Intake> Intakes { get; set; } = new List<Intake>();
        public virtual ICollection<ScheduleTime> ScheduleTimes { get; set; } = new List<ScheduleTime>();
        public virtual ICollection<ScheduleWeekDay> ScheduleWeekDays { get; set; } = new List<ScheduleWeekDay>();
    }
}