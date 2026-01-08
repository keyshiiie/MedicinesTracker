using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using System.Collections.ObjectModel;
using System.Linq;

namespace MedicinesTracker.Modules.Medications.Models
{
    public class ScheduleFieldCleaner
    {
        // Очистка при смене ТИПА расписания
        public void CleanForScheduleType(
            string? newTypeCode,
            string? oldTypeCode,
            ref MedicineScheduleDto scheduleDto,
            ref ScheduleModeModel? selectedMode,
            ref RecurrencePatternModel? selectedPattern,
            ref ObservableCollection<WeekDayModel> weekDays)
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
            ref ObservableCollection<WeekDayModel> weekDays,
            ref RecurrencePatternModel? selectedPattern)
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