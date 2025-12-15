using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models.Dto
{
    public class MedicineDetailDto
    {
        public int IdMedicine { get; set; }
        public string? MedicineName { get; set; }
        public string? MethodAdmissionName { get; set; }
        public string? UnitName { get; set; }
        public int RecipientId{ get; set; }
        public string? RecipientName { get; set; }

        // Свойства из Stock
        public int? IdStock { get; set; }
        public int? InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public int Threshold { get; set; }
        public bool ReminderEnabled { get; set; }
    }

}
