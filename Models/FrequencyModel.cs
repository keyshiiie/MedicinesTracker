namespace MedicinesTracker.Models
{
    public class FrequencyModel
    {
        public string? Name { get; set; }

        public FrequencyModel() { }
        public FrequencyModel(string name)
        {
            Name = name;
        }
    }
}
