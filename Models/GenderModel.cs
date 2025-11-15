namespace MedicinesTracker.Models
{
    public class GenderModel
    {
        public string? Name { get; set; }

        public GenderModel() { }
        public GenderModel(string name)
        {
            Name = name;
        }
    }
}
