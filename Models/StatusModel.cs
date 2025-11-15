namespace MedicinesTracker.Models
{
    public class StatusModel
    {
        public string? Name { get; set; }

        public StatusModel() { }
        public StatusModel(string name)
        {
            Name = name;
        }
    }
}
