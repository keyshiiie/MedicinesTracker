using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models
{
    public class OneTimeReminderModel
    {
        public int IdOnetimeReminder { get; set; }
        public int IdMedicine { get; set; }     

        public string? Date { get; set; }   
        public string? Time { get; set; }     
        public int Dosage { get; set; }

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
