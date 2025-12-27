using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Modules.Medications.Models
{
    public class ScheduleFieldCleaner
    {
        // Очистка при смене ТИПА расписания
        public void CleanForScheduleType(
            string? newTypeCode,
            string? oldTypeCode,
            MedicineScheduleDto scheduleDto,
            ref ScheduleModeModel? selectedMode,
            ref RecurrencePatternModel? selectedPattern,
            ref WeekDayModel? selectedWeekDay)
        {
            // Если переключились с RECURRING на ONETIME
            if (oldTypeCode == "RECURRING" && newTypeCode == "ONETIME")
            {
                selectedMode = null;
                selectedPattern = null;
                selectedWeekDay = null;
                scheduleDto.DateStart = string.Empty;
                scheduleDto.DateEnd = string.Empty;
            }
            // Если переключились с ONETIME на RECURRING
            else if (oldTypeCode == "ONETIME" && newTypeCode == "RECURRING")
            {
                scheduleDto.OneTimeDate = string.Empty;
            }
        }

        // Очистка при смене РЕЖИМА расписания
        public void CleanForScheduleMode(
            string? newModeCode,
            string? oldModeCode,
            ref WeekDayModel? selectedWeekDay,
            ref RecurrencePatternModel? selectedPattern)
        {
            // Если переключились с INTERVAL на WEEKDAYS
            if (oldModeCode == "INTERVAL" && newModeCode == "WEEKDAYS")
            {
                selectedPattern = null;
            }
            // Если переключились с WEEKDAYS на INTERVAL
            else if (oldModeCode == "WEEKDAYS" && newModeCode == "INTERVAL")
            {
                selectedWeekDay = null;
            }
        }
    }
}
