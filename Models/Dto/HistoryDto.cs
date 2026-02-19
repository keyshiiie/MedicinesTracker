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
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? TakenDateTime { get; set; }
        public string? ActualDosage { get; set; }
        public string? UnitName { get; set; }
        public int IdRecipient { get; set; }
        public string RecipientName { get; set; } = string.Empty;

        // Вычисляемые свойства (нужные для UI)
        public string TakenTimeFormatted
        {
            get
            {
                if (DateTime.TryParse(TakenDateTime, out var dateTime))
                    return dateTime.ToString("HH:mm");
                return string.Empty;
            }
        }

        public string Status
        {
            get
            {
                if (IsCompleted)
                    return "Принято";

                var now = DateTime.Now;
                if (!DateTime.TryParse($"{Date} {Time}", out var planned))
                    return "Неизвестно";

                if (planned.Date > now.Date)
                    return "Запланировано";

                if (planned.Date == now.Date)
                    return planned.TimeOfDay > now.TimeOfDay ? "Ожидает" : "Пропущено";

                return "Пропущено";
            }
        }

        public Color StatusColor => Status switch
        {
            "Принято" => Color.FromArgb("#52795D"),
            "Пропущено" => Color.FromArgb("#795252"),
            "Ожидает" => Color.FromArgb("#796752"),
            "Запланировано" => Color.FromArgb("#6F8C72"),
            _ => Color.FromArgb("#1D1E1E"),
        };
    }
}