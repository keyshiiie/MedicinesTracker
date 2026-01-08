namespace MedicinesTracker.Models.Dto
{
    public class MedicineScheduleDto
    {
        public int IdSchedule { get; set; }
        public int IdMedicine { get; set; }
        public string? MedicineName { get; set; }
        public string? UnitName { get; set; }

        // Тип расписания
        public int IdScheduleType { get; set; }
        public string? ScheduleTypeCode { get; set; }
        public string? ScheduleTypeName { get; set; }

        // Режим расписания
        public int? IdScheduleMode { get; set; }
        public string? ScheduleModeCode { get; set; }
        public string? ScheduleModeName { get; set; }

        // Периодичность
        public int? IdRecurrencePattern { get; set; }
        public string? RecurrencePatternName { get; set; }
        public int? DaysInterval { get; set; }

        // Расписание
        public string? OneTimeDate { get; set; }
        public int Dosage { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public bool ScheduleIsActive { get; set; }

        // Дни недели
        public string? WeekDays { get; set; }

        // Время приема
        public string? Times { get; set; }
        public string? TimeOrders { get; set; }
    }
}