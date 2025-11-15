namespace MedicinesTracker.Models
{
    public class UnitModel
    {
        public string? Name { get; set; }

        public UnitModel() { }
        public UnitModel(string name)
        {
            Name = name;
        }
    }
}
