using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Modules.Medications.Models
{
    public class ScheduleValidator
    {
        public List<string> GetValidationErrors(
        bool isRecurring,
        bool isIntervalMode,
        bool isWeekDaysMode,
        string? dateStart,
        string? oneTimeDate,
        ScheduleTypeModel? selectedType,
        ScheduleModeModel? selectedMode,
        RecurrencePatternModel? selectedPattern,
        WeekDayModel? selectedWeekDay)
        {
            var errors = new List<string>();

            if (selectedType == null)
                errors.Add("Выберите тип расписания");

            if (isRecurring)
            {
                if (selectedMode == null)
                    errors.Add("Выберите способ задания расписания");

                if (isIntervalMode && selectedPattern == null)
                    errors.Add("Выберите частоту приёма");

                if (isWeekDaysMode && selectedWeekDay == null)
                    errors.Add("Выберите день недели");

                if (string.IsNullOrEmpty(dateStart))
                    errors.Add("Укажите дату начала приёма");
            }
            else
            {
                if (string.IsNullOrEmpty(oneTimeDate))
                    errors.Add("Укажите дату приёма");
            }

            return errors;
        }
    }
}
