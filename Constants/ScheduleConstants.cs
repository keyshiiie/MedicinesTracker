using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Constants
{
    public static class ScheduleTypes
    {
        public const string OneTime = "ONETIME";
        public const string Recurring = "RECURRING";

        public static int GetId(string code) => code switch
        {
            OneTime => 2,
            Recurring => 1,
            _ => 1
        };

        public static string GetName(string code) => code switch
        {
            OneTime => "Одноразовое",
            Recurring => "Повторяющееся",
            _ => "Неизвестно"
        };
    }

    public static class ScheduleModes
    {
        public const string Interval = "INTERVAL";
        public const string Weekly = "WEEKDAYS";

        public static int? GetId(string code) => code switch
        {
            Interval => 1,
            Weekly => 2,
            _ => null
        };

        public static string GetName(string code) => code switch
        {
            Interval => "Интервал",
            Weekly => "Дни недели",
            _ => "Неизвестно"
        };
    }
}
