using MedicinesTracker.Interface;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public class MedicineService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IRecipientRepository _recipientRepository;
        private readonly IMethodAdmissionRepository _methodAdmissionRepository;

        public MedicineService(IMedicineRepository medicineRepository, IUnitRepository unitRepository, IRecipientRepository recipientRepository, IMethodAdmissionRepository methodAdmissionRepository)
        {
            _medicineRepository = medicineRepository;
            _unitRepository = unitRepository;
            _recipientRepository = recipientRepository; 
            _methodAdmissionRepository = methodAdmissionRepository;
        }

        public async Task<IEnumerable<MedicineDetailDto>> GetAllMedicineDetailsAsync()
        {
            var data = await _medicineRepository.GetMedicineDetailsAsync();
            Debug.WriteLine($"Лекарства. Загружено записей: {data.Count()}");
            return data;
        }

        public async Task<IEnumerable<UnitModel>> GetAllUnitsAsync()
        {
            var data = await _unitRepository.GetAllUnitsAsync();
            Debug.WriteLine($"Единицы измерения. Загружено записей: {data.Count()}");
            return data;
        }
        public async Task<IEnumerable<RecipientModel>> GetAllRecipientsAsync()
        {
            var data = await _recipientRepository.GetAllRecipientsAsync();
            Debug.WriteLine($"Получатели лекарства. Загружено записей: {data.Count()}");
            return data;
        }

        public async Task<IEnumerable<MethodAdmissionModel>> GetAllMethodsAdmissionAsync()
        {
            var data = await _methodAdmissionRepository.GetAllMethodsAdmissionAsync();
            Debug.WriteLine($"Способы принятия лекарства. Загружено записей: {data.Count()}");
            return data;
        }

        public async Task<int> EditMedicineAsync(MedicineDto medicineDto)
        {
            if (medicineDto == null)
                throw new ArgumentNullException(nameof(medicineDto));

            var rowsAffected = await _medicineRepository.UpdateMedicineAsync(medicineDto);
            Debug.WriteLine($"Обновление лекарства. Затронуто строк: {rowsAffected}");
            return rowsAffected;
        }

    }
}
