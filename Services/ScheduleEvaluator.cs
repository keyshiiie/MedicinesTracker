using MedicinesTracker.Dto;

namespace MedicinesTracker.Services
{
    public interface IScheduleEvaluator
    {
        bool ShouldTakeOnDate(MedicineWithScheduleDto medicine, DateTime date);
        string[] GetMedicationTimes(MedicineWithScheduleDto medicine);
        bool IsScheduleActiveOnDate(MedicineWithScheduleDto medicine, DateTime date);
    }

    public class ScheduleEvaluator : IScheduleEvaluator
    {
        public bool ShouldTakeOnDate(MedicineWithScheduleDto medicine, DateTime date)
        {
            if (medicine == null || !medicine.ScheduleIsActive)
                return false;

            var dateStr = date.ToString("yyyy-MM-dd");

            // Одноразовое расписание
            if (medicine.ScheduleTypeCode == "ONETIME")
            {
                return medicine.OneTimeDate == dateStr;
            }

            // Повторяющееся расписание
            if (medicine.ScheduleTypeCode == "RECURRING")
            {
                // Проверяем период действия
                if (!string.IsNullOrEmpty(medicine.DateStart))
                {
                    var startDate = DateTime.Parse(medicine.DateStart);
                    if (date < startDate) return false;
                }

                if (!string.IsNullOrEmpty(medicine.DateEnd))
                {
                    var endDate = DateTime.Parse(medicine.DateEnd);
                    if (date > endDate) return false;
                }

                // Интервальное расписание
                if (medicine.ScheduleModeCode == "INTERVAL" && medicine.DaysInterval.HasValue)
                {
                    DateTime referenceDate;
                    if (!string.IsNullOrEmpty(medicine.OneTimeDate))
                    {
                        referenceDate = DateTime.Parse(medicine.OneTimeDate);
                    }
                    else if (!string.IsNullOrEmpty(medicine.DateStart))
                    {
                        referenceDate = DateTime.Parse(medicine.DateStart);
                    }
                    else
                    {
                        return false;
                    }

                    var daysDiff = (date - referenceDate).Days;
                    return daysDiff >= 0 && daysDiff % medicine.DaysInterval.Value == 0;
                }

                // Расписание по дням недели
                if (medicine.ScheduleModeCode == "WEEKDAYS" && !string.IsNullOrEmpty(medicine.WeekDayIds))
                {
                    var dayNumber = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var dayIds = medicine.WeekDayIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var num) ? num : 0)
                        .Where(id => id > 0);

                    return dayIds.Contains(dayNumber);
                }

                // Если режим не указан - каждый день
                return true;
            }

            return false;
        }

        public string[] GetMedicationTimes(MedicineWithScheduleDto medicine)
        {
            if (medicine == null || string.IsNullOrEmpty(medicine.Times))
                return Array.Empty<string>();

            return medicine.Times
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        public bool IsScheduleActiveOnDate(MedicineWithScheduleDto medicine, DateTime date)
        {
            if (medicine == null || !medicine.ScheduleIsActive)
                return false;

            // Проверяем период действия
            if (!string.IsNullOrEmpty(medicine.DateStart))
            {
                var startDate = DateTime.Parse(medicine.DateStart);
                if (date < startDate) return false;
            }

            if (!string.IsNullOrEmpty(medicine.DateEnd))
            {
                var endDate = DateTime.Parse(medicine.DateEnd);
                if (date > endDate) return false;
            }

            return true;
        }
    }
}
