using System.ComponentModel.DataAnnotations;

namespace MedicinesTracker.Entities
{
    public class AppSetting
    {
        [Key]
        public int Id { get; set; }

        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}