using System;
using System.Collections.Generic;
using System.Text;
using MedicinesTracker.Constants;

namespace MedicinesTracker.Services.Navigation
{
    public interface IMedicationCreationNavigationService
    {
        Task ToBaseInfoAsync(int? medicineId = null);
        Task ToStockInfoAsync(int medicineId, string unitName, int? stockId = null);
        Task ToScheduleTypeSelectionAsync(int medicineId, bool isNewMedicine);
        Task ToScheduleModeSelectionAsync(string scheduleTypeCode, int medicineId, bool isNewMedicine);
        Task ToScheduleDetailsAsync(string scheduleTypeCode, string? scheduleModeCode,
                                    int medicineId, bool isNewMedicine, int? scheduleId = null);
        Task BackToMedicineListAsync();
    }

    public class MedicationCreationNavigationService : IMedicationCreationNavigationService
    {
        private readonly INavigationService _navigation;

        public MedicationCreationNavigationService(INavigationService navigation)
        {
            _navigation = navigation;
        }

        public async Task ToBaseInfoAsync(int? medicineId = null)
        {
            var parameters = new Dictionary<string, object>();
            if (medicineId.HasValue && medicineId.Value > 0)
            {
                parameters["idMedicine"] = medicineId.Value;
            }

            await _navigation.GoToAsync(NavigationRoutes.BaseInfo, parameters);
        }

        public async Task ToStockInfoAsync(int medicineId, string unitName, int? stockId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["idMedicine"] = medicineId,
                ["unitName"] = unitName ?? string.Empty,
                ["idStock"] = stockId ?? 0
            };

            await _navigation.GoToAsync(NavigationRoutes.StockInfo, parameters);
        }

        public async Task ToScheduleTypeSelectionAsync(int medicineId, bool isNewMedicine)
        {
            var parameters = new Dictionary<string, object>
            {
                ["medicineId"] = medicineId,
                ["isNewMedicine"] = isNewMedicine
            };

            await _navigation.GoToAsync(NavigationRoutes.ScheduleTypeSelection, parameters);
        }

        public async Task ToScheduleModeSelectionAsync(string scheduleTypeCode,
            int medicineId, bool isNewMedicine)
        {
            var parameters = new Dictionary<string, object>
            {
                ["scheduleTypeCode"] = scheduleTypeCode,
                ["medicineId"] = medicineId,
                ["isNewMedicine"] = isNewMedicine
            };

            await _navigation.GoToAsync(NavigationRoutes.ScheduleModeSelection, parameters);
        }

        public async Task ToScheduleDetailsAsync(string scheduleTypeCode,
            string? scheduleModeCode, int medicineId, bool isNewMedicine, int? scheduleId = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["scheduleTypeCode"] = scheduleTypeCode,
                ["medicineId"] = medicineId,
                ["isNewMedicine"] = isNewMedicine,
                ["scheduleId"] = scheduleId ?? 0
            };

            if (!string.IsNullOrEmpty(scheduleModeCode))
            {
                parameters["scheduleModeCode"] = scheduleModeCode;
            }

            await _navigation.GoToAsync(NavigationRoutes.ScheduleDetails, parameters);
        }

        public async Task BackToMedicineListAsync()
        {
            await _navigation.GoToAsync(NavigationRoutes.MedicineList);
        }
    }
}