using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicinesTracker.Entities
{
    public class NotificationSetting
    {
        [Key]
        public int IdNotificationSetting { get; set; }

        public int IdRecipient { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Sound { get; set; } = "default";

        // Навигационное свойство
        [ForeignKey(nameof(IdRecipient))]
        public virtual Recipient Recipient { get; set; } = null!;
    }
}