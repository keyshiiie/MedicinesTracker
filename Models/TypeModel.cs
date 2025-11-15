namespace MedicinesTracker.Models
{
    public class TypeModel
    {
        public string? Name { get; set; }

        public TypeModel() { }
        public TypeModel(string name)
        {
            Name = name;
        }
    }
}
