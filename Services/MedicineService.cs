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
        private readonly IStockRepository _stockRepository;
        private readonly IReminderRepository _reminderRepository;

        public MedicineService(IMedicineRepository medicineRepository, 
            IUnitRepository unitRepository, 
            IRecipientRepository recipientRepository, 
            IMethodAdmissionRepository methodAdmissionRepository,
            IStockRepository stockRepository,
            IReminderRepository reminderRepository)
        {
            _medicineRepository = medicineRepository;
            _unitRepository = unitRepository;
            _recipientRepository = recipientRepository; 
            _methodAdmissionRepository = methodAdmissionRepository;
            _stockRepository = stockRepository;
            _reminderRepository = reminderRepository;
        }

        public async Task<IEnumerable<MedicineDetailDto>> GetAllMedicineDetailsAsync()
        {
            var data = await _medicineRepository.GetMedicineDetailsAsync();
            Debug.WriteLine($"Лекарства. Загружено записей: {data.Count()}");
            return data;
        }

        public async Task<IEnumerable<GroupedReminderDto>> GetAllRemindersByMedicineIdAsync(int medicineId)
        {
            var data = await _reminderRepository.GetGroupedRemindersByMedicineIdAsync(medicineId);
            Debug.WriteLine($"Напоминания для лекарства. Загружено записей: {data.Count()}");
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

        public async Task<int> EditMedicineAsync(MedicineModel medicineModel)
        {
            if (medicineModel == null)
                throw new ArgumentNullException(nameof(medicineModel));

            var rowsAffected = await _medicineRepository.UpdateMedicineAsync(medicineModel);
            Debug.WriteLine($"Обновление лекарства. Затронуто строк: {rowsAffected}");
            return rowsAffected;
        }

        public async Task<int> EditStockAsync(StockModel stockModel)
        {
            if(stockModel == null)
                throw new ArgumentNullException(nameof(stockModel));
            var rowsAffected = await _stockRepository.UpdateStockAsync(stockModel);
            Debug.WriteLine($"Обновление запаса лекарства. Затронуто строк: {rowsAffected}");
            return rowsAffected;
        }
    }
}
