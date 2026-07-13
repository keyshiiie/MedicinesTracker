using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public class Recipient
    {
        [Key]
        public int IdRecipient { get; set; }

        public string? Name { get; set; }
        public int IdGender { get; set; }
        public int Age { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }

        // Обратные навигации
        public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
        public virtual NotificationSetting NotificationSetting { get; set; } = null!;
    }
}