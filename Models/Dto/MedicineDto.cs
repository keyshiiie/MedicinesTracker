using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models.Dto
{
    public class MedicineDto
    {
        public int? IdMedicine { get; set; }  // Nullable для создания
        public string? Name { get; set; }
        public int IdUnit { get; set; }
        public int IdMethodAdmission { get; set; }
        public int IdRecipient { get; set; }
    }
}
