namespace MedicinesTracker.Models.Dto
{
    public class RecipientFilter
    {
        public string Name { get; set; }

        public RecipientFilter(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
}