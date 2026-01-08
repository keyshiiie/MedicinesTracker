using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Models.Dto
{
    public class HistoryDto
    {
        public int IdIntake { get; set; }
        public int IdMedicine { get; set; }
        public string? NameMedicine { get; set; }
        public bool IsCompleted { get; set; }
        public int IdSchedule { get; set; }
        public int IdScheduleTime { get; set; }
        public int OrderInDay { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? ActualDosage { get; set; }
        public string? UnitName { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string? Status { get; set; }
        public Color StatusColor
        {
            get => Status switch
            {
                "Принято" => Color.FromArgb("#52795D"),
                "Пропущено" => Color.FromArgb("#795252"),
                "Ожидает" => Color.FromArgb("#796752"),
                "Запланировано" => Color.FromArgb("#6F8C72"),
                _ => Color.FromArgb("#1D1E1E"),
            };
        }
    }
}
