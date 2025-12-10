using MedicinesTracker.Models.Dto;

namespace MedicinesTracker.Interface
{
    public interface IMedicineRepository
    {
        Task<IEnumerable<MedicineDetailDto>> GetMedicineDetailsAsync();
        Task<int> UpdateMedicineAsync(MedicineDto medicineDto);
    }
}
