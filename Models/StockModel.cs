using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models
{
    public class StockModel
    {
        public int IdStock { get; set; }
        public int IdMedicine { get; set; }     

        public int InitialQuantity { get; set; }
        public int Threshold { get; set; }
        public int CurrentQuantity { get; set; }
        public bool ReminderEnabled { get; set; }

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
