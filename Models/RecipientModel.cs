namespace MedicinesTracker.Models
{
    public class RecipientModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public GenderModel? Gender { get; set; }
        public int Age { get; set; }
        public RecipientModel() { }
        public RecipientModel(int id, string name, GenderModel gender, int age)
        {
            Id = id;
            Name = name;
            Gender = gender;
            Age = age;
        }
    }
}
