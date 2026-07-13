using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class Stock
    {
        [Key]
        public int IdStock { get; set; }

        public int IdMedicine { get; set; }
        public int? Threshold { get; set; }
        public int? CurrentQuantity { get; set; }
        public bool ReminderEnabled { get; set; } = true;

        // Навигационное свойство
        [ForeignKey(nameof(IdMedicine))]
        public virtual Medicine Medicine { get; set; } = null!;
    }
}