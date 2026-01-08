namespace MedicinesTracker.Models.Dto
{
    public class MedicineWithScheduleDto
    {
        public int IdMedicine { get; set; }
        public string? MedicineName { get; set; }
        public string? RecipientName { get; set; }
        public string? UnitName { get; set; }
        public int IdUnit { get; set; }
        public int IdRecipient { get; set; }

        // Расписание
        public int IdSchedule { get; set; }
        public int IdScheduleType { get; set; }
        public string? ScheduleTypeCode { get; set; } // "ONETIME", "RECURRING"
        public string? ScheduleTypeName { get; set; }

        public int? IdScheduleMode { get; set; }
        public string? ScheduleModeCode { get; set; } // "INTERVAL", "WEEKDAYS"
        public string? ScheduleModeName { get; set; }

        public int? IdRecurrencePattern { get; set; }
        public int? DaysInterval { get; set; }
        public string? RecurrencePatternName { get; set; }

        public string? OneTimeDate { get; set; }
        public int Dosage { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public bool ScheduleIsActive { get; set; }

        // Дни недели
        public string? WeekDayIds { get; set; } // "1,2,3,4,5"
        public string? WeekDays { get; set; }   // "Понедельник,Вторник,Среда"

        // Время приема
        public string? Times { get; set; }      // "08:00,12:00,20:00"
        public string? TimeOrders { get; set; } // "1,2,3"
    }
}