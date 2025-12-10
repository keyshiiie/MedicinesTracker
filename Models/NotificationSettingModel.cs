using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models
{
    public class NotificationSettingModel
    {
        public int IdNotificationSetting { get; set; }
        public int IdRecipient { get; set; } 
        public bool IsEnabled { get; set; }
        public string? Sound { get; set; }

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
