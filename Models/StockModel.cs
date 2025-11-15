using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.Models
{
    public class StockModel
    {
        public int Id { get; set; }
        public MedicineModel? MedicineModel { get; set; }
        public int InitialQuantity { get; set; }
        public int Threshold { get; set; }
        public int CurrentQuantity { get; set; }
        public bool ReminderEnabled { get; set;}

        public StockModel() { }
        public StockModel(int id, MedicineModel medicineModel, int initialQuantity, int threshold, int currentQuantity, bool isReminderEnabled)
        {
            Id = id;
            MedicineModel = medicineModel;
            InitialQuantity = initialQuantity;
            Threshold = threshold;
            CurrentQuantity = currentQuantity;
            ReminderEnabled = isReminderEnabled;
        }
    }
}
