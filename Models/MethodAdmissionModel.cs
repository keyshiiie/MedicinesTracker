namespace MedicinesTracker.Models
{
    public class MethodAdmissionModel
    {
        public string? Name { get; set; }

        public MethodAdmissionModel() { }
        public MethodAdmissionModel(string name)
        {
            Name = name;
        }
    }
}
