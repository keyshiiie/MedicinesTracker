namespace MedicinesTracker.Models
{
    public class IntakeModel
    {
        public int IdIntake { get; set; }
        public int IdType { get; set; }    
        public int IdMedicine { get; set; }   
        public int IdStatus { get; set; } 

        public string? Date { get; set; }  
        public string? Time { get; set; }     

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
