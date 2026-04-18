using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using System.Diagnostics;
using MedicinesTracker.Dto;

namespace MedicinesTracker.Services
{
    public interface IMedicineBuilder
    {
        IMedicineBuilder WithBaseInfo(Medicine medicine);
        IMedicineBuilder WithStockInfo(Stock stock);
        IMedicineBuilder WithSchedule(MedicineScheduleDto schedule,
            List<WeekDay> selectedDays,
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
        private readonly ITransactionHandler _transactionHandler;

        private Medicine? _medicine;
        private Stock? _stock;
        private MedicineScheduleDto? _schedule;
        private List<WeekDay>? _selectedDays;
        private List<TimeSpan>? _selectedTimes;

        public bool IsComplete => _medicine != null && _stock != null && _schedule != null;

        public MedicineBuilder(
            IMedicineRepository medicineRepository,
            IStockRepository stockRepository,
            IScheduleService scheduleService,
            ITransactionHandler transactionHandler)
        {
            _medicineRepository = medicineRepository;
            _stockRepository = stockRepository;
            _scheduleService = scheduleService;
            _transactionHandler = transactionHandler;
        }

        public IMedicineBuilder WithBaseInfo(Medicine medicine)
        {
            _medicine = medicine ?? throw new ArgumentNullException(nameof(medicine));
            return this;
        }

        public IMedicineBuilder WithStockInfo(Stock stock)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            return this;
        }

        public IMedicineBuilder WithSchedule(MedicineScheduleDto schedule,
            List<WeekDay> selectedDays,
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

            return await _transactionHandler.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    // 1. Сохраняем лекарство
                    var medicineId = await _medicineRepository.AddMedicineAsync(_medicine!);

                    // 2. Сохраняем запас
                    if (_stock != null)
                    {
                        _stock.IdMedicine = medicineId;
                        await _stockRepository.AddStockAsync(_stock);
                    }

                    // 3. Сохраняем расписание
                    if (_schedule != null)
                    {
                        _schedule.IdMedicine = medicineId;
                        await _scheduleService.SaveScheduleAsync(
                            _schedule,
                            _selectedDays!,
                            _selectedTimes!);
                    }

                    Debug.WriteLine($"✅ Лекарство успешно создано: ID={medicineId}");

                    // 4. Сбрасываем builder
                    Reset();

                    return medicineId;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка создания лекарства: {ex.Message}");
                    throw;
                }
            });
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