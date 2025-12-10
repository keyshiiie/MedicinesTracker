namespace MedicinesTracker.Models
{
    public class MedicineModel
    {
        public int IdMedicine { get; set; }
        public string? Name { get; set; }

        public int IdUnit { get; set; }
        public int IdMethodAdmission { get; set; }
        public int IdRecipient { get; set; }

        public string? CreatedAt { get; set; } 
        public string? UpdatedAt { get; set; }


    }
}
