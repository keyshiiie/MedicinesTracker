namespace MedicinesTracker.Models
{
    public class RecipientModel
    {
        public int IdRecipient { get; set; }
        public string? Name { get; set; }
        public int IdGender { get; set; }
        public int Age { get; set; }

        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
    }
}
