using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using System.Collections.ObjectModel;

namespace MedicinesTracker.Modules.Medications.s
{
    public class ScheduleFieldCleaner
    {
        // Очистка при смене ТИПА расписания
        public void CleanForScheduleType(
            string? newTypeCode,
            string? oldTypeCode,
            ref MedicineScheduleDto scheduleDto,
            ref ScheduleMode? selectedMode,
            ref RecurrencePattern? selectedPattern,
            ref ObservableCollection<WeekDay> weekDays)
        {
            if (oldTypeCode == "RECURRING" && newTypeCode == "ONETIME")
            {
                selectedMode = null;
                selectedPattern = null;
                scheduleDto.DateStart = null;
                scheduleDto.DateEnd = null;
                scheduleDto.WeekDays = null;

                // Сбрасываем выбранные дни
                if (weekDays != null)
                {
                    foreach (var day in weekDays)
                    {
                        day.IsSelected = false;
                    }
                }
            }
            else if (oldTypeCode == "ONETIME" && newTypeCode == "RECURRING")
            {
                scheduleDto.OneTimeDate = null;
            }
        }

        // Очистка при смене РЕЖИМА расписания
        public void CleanForScheduleMode(
            string? newModeCode,
            string? oldModeCode,
            ref ObservableCollection<WeekDay> weekDays,
            ref RecurrencePattern? selectedPattern)
        {
            if (oldModeCode == "INTERVAL" && newModeCode == "WEEKDAYS")
            {
                selectedPattern = null;
            }
            else if (oldModeCode == "WEEKDAYS" && newModeCode == "INTERVAL")
            {
                // Сбрасываем выбранные дни при переключении с WEEKDAYS на INTERVAL
                if (weekDays != null)
                {
                    foreach (var day in weekDays)
                    {
                        day.IsSelected = false;
                    }
                }
            }
        }
    }
}