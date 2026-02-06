using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MedicinesTracker.Services
{
    public interface IMedicineBuilder
    {
        IMedicineBuilder WithBaseInfo(MedicineModel medicine);
        IMedicineBuilder WithStockInfo(StockModel stock);
        IMedicineBuilder WithSchedule(MedicineScheduleDto schedule,
            List<WeekDayModel> selectedDays,
            List<TimeSpan> selectedTimes);
        Task<int> BuildAsync();
        void Reset();
        bool IsComplete { get; }
        MedicineCreationState GetState();

    }

    public class MedicineBuilder : IMedicineBuilder
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IScheduleService _scheduleService;
        private readonly IDBHandler _dbHandler;
        private readonly ITransactionHandler _transactionHandler;

        private MedicineModel? _medicine;
        private StockModel? _stock;
        private MedicineScheduleDto? _schedule;
        private List<WeekDayModel>? _selectedDays;
        private List<TimeSpan>? _selectedTimes;

        public bool IsComplete => _medicine != null && _stock != null && _schedule != null;

        public MedicineBuilder(
            IMedicineRepository medicineRepository,
            IStockRepository stockRepository,
            IScheduleService scheduleService,
            IDBHandler dbHandler,
            ITransactionHandler transactionHandler)
        {
            _medicineRepository = medicineRepository;
            _stockRepository = stockRepository;
            _scheduleService = scheduleService;
            _dbHandler = dbHandler;
            _transactionHandler = transactionHandler;
        }

        public IMedicineBuilder WithBaseInfo(MedicineModel medicine)
        {
            _medicine = medicine ?? throw new ArgumentNullException(nameof(medicine));
            return this;
        }

        public IMedicineBuilder WithStockInfo(StockModel stock)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            return this;
        }

        public IMedicineBuilder WithSchedule(MedicineScheduleDto schedule,
            List<WeekDayModel> selectedDays,
            List<TimeSpan> selectedTimes)
        {
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            _selectedDays = selectedDays ?? throw new ArgumentNullException(nameof(selectedDays));
            _selectedTimes = selectedTimes ?? throw new ArgumentNullException(nameof(selectedTimes));
            return this;
        }

        public async Task<int> BuildAsync()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Не все данные заполнены для создания лекарства");

            try
            {
                // ВАЖНО: Сначала получаем MedicineId из builder
                if (_medicine == null)
                    throw new InvalidOperationException("Базовая информация о лекарстве не заполнена");

                // 1. Сохраняем лекарство
                var medicineId = await _medicineRepository.AddMedicineAsync(_medicine);

                // 2. Сохраняем запас
                if (_stock != null)
                {
                    await _stockRepository.AddStockAsync(_stock, medicineId);
                }

                // 3. Сохраняем расписание
                if (_schedule != null && _selectedDays != null && _selectedTimes != null)
                {
                    _schedule.IdMedicine = medicineId;

                    // Используем ScheduleService для сохранения расписания
                    // (убедитесь, что ScheduleService тоже работает в транзакции)
                    await _scheduleService.SaveScheduleAsync(
                        _schedule,
                        _selectedDays,
                        _selectedTimes);
                }

                // 4. Сбрасываем builder
                Reset();

                Debug.WriteLine($"✅ Лекарство успешно создано: ID={medicineId}");
                return medicineId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка создания лекарства: {ex.Message}");
                throw new Exception($"Не удалось создать лекарство: {ex.Message}", ex);
            }
        }

        public void Reset()
        {
            _medicine = null;
            _stock = null;
            _schedule = null;
            _selectedDays = null;
            _selectedTimes = null;
        }

        public MedicineCreationState GetState()
        {
            return new MedicineCreationState
            {
                Medicine = _medicine,
                Stock = _stock,
                Schedule = _schedule,
                SelectedDays = _selectedDays,
                SelectedTimes = _selectedTimes,
                IsComplete = IsComplete
            };
        }
    }
}
