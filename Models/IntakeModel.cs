namespace MedicinesTracker.Models
{
    public class IntakeModel
    {
        public int Id { get; set; }
        public TypeModel? Type { get; set; }
        public MedicineModel? Medicine { get; set; }
        public StatusModel? Status { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }

        public IntakeModel() { }
        public IntakeModel(int id, TypeModel type, MedicineModel medicine, StatusModel status, DateOnly date, TimeOnly time)
        {
            Id = id;
            Type = type;
            Medicine = medicine;
            Status = status;
            Date = date;
            Time = time;
        }
    }
}
