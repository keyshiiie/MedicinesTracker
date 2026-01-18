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
    public string? TakenDateTime { get; set; }
    public string? ActualDosage { get; set; }
    public string? UnitName { get; set; }
    public int IdRecipient { get; set; }
    public string RecipientName { get; set; } = string.Empty;

    // Вычисляемые свойства
    public DateTime PlannedDateTime
    {
        get
        {
            try
            {
                if (string.IsNullOrEmpty(Date) || string.IsNullOrEmpty(Time))
                    return DateTime.MinValue;

                var timePart = Time.Length >= 5 ? Time.Substring(0, 5) : Time;
                return DateTime.Parse($"{Date} {timePart}");
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }

    public DateTime? ActualDateTime
    {
        get
        {
            try
            {
                if (string.IsNullOrEmpty(TakenDateTime) || TakenDateTime == "null")
                    return null;

                return DateTime.Parse(TakenDateTime);
            }
            catch
            {
                return null;
            }
        }
    }

    // ФОРМАТИРОВАННАЯ ДАТА ПРИЁМА
    public string FormattedTakenDateTime
        => ActualDateTime.HasValue
            ? $"Принято: {ActualDateTime.Value:dd.MM.yyyy HH:mm}"
            : string.Empty;

    // Упрощенная и исправленная логика статуса
    public string Status
    {
        get
        {
            // ПЕРВОЕ и ГЛАВНОЕ условие - если IsCompleted = true
            if (IsCompleted)
            {
                return "Принято";
            }

            // Если не принято, проверяем время
            var now = DateTime.Now;
            var planned = PlannedDateTime;

            // Проверяем валидность PlannedDateTime
            if (planned == DateTime.MinValue)
                return "Неизвестно";

            // Если запись на будущее (дата больше текущей)
            if (planned.Date > now.Date)
                return "Запланировано";

            // Если запись сегодня, проверяем время
            if (planned.Date == now.Date)
            {
                // Время еще не наступило
                if (planned.TimeOfDay > now.TimeOfDay)
                    return "Ожидает";

                // Время уже прошло
                return "Пропущено";
            }

            // Если запись в прошлом (дата меньше текущей)
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

    // Дополнительные полезные свойства
    public bool IsMissed => Status == "Пропущено";
    public bool IsUpcoming => Status == "Ожидает" || Status == "Запланировано";
    public bool CanBeTaken => !IsCompleted && (Status == "Ожидает" || Status == "Пропущено");
}