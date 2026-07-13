using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Constants
{
    public static class NavigationRoutes
    {
        // Модуль Medications
        public const string BaseInfo = "BaseInfoPage";
        public const string StockInfo = "StockInfoPage";
        public const string ScheduleTypeSelection = "ScheduleTypeSelectionPage";
        public const string ScheduleModeSelection = "ScheduleModeSelectionPage";
        public const string ScheduleDetails = "ScheduleDetailsPage";
        public const string MedicineList = "//medicines";

        // Другие модули
        public const string Settings = "SettingsPage";
        public const string MedicineDetail = "MedicineDetailPage";
        public const string EditRecipient = "EditRecipientPage";

        // Специальные
        public const string Back = "..";
    }
}