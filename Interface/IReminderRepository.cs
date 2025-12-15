using MedicinesTracker.Models.Dto;

namespace MedicinesTracker.Interface
{
    public interface IReminderRepository
    {
        Task<IEnumerable<GroupedReminderDto>> GetGroupedRemindersByMedicineIdAsync(int medicineId);
    }
}
