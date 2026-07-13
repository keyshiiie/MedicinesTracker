using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public class Unit
    {
        [Key]
        public int IdUnit { get; set; }

        public string? Name { get; set; }

        // Обратная навигация
        public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }
}