using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class Medicine
    {
        [Key]
        public int IdMedicine { get; set; }

        public string? Name { get; set; }
        public int IdUnit { get; set; }
        public int IdMethodAdmission { get; set; }
        public int IdRecipient { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }

        // Навигационные свойства
        [ForeignKey(nameof(IdUnit))]
        public virtual Unit Unit { get; set; } = null!;

        [ForeignKey(nameof(IdMethodAdmission))]
        public virtual MethodAdmission MethodAdmission { get; set; } = null!;

        [ForeignKey(nameof(IdRecipient))]
        public virtual Recipient Recipient { get; set; } = null!; 

        // Коллекции для обратных связей
        public virtual ICollection<Intake> Intakes { get; set; } = new List<Intake>();
        public virtual ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();
        public virtual Stock Stock { get; set; } = null!;
    }
}