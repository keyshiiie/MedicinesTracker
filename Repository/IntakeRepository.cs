using MedicinesTracker.Data;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IIntakeRepository
    {
        // CRUD
        Task<IEnumerable<HistoryDto>> GetAllIntakeAsync();
        Task<int> AddIntakeAsync(Intake intake);
        Task<int> UpdateIntakeAsync(Intake intake);
        Task<Intake?> GetIntakeByIdAsync(int intakeId);

        // Поиск
        Task<IEnumerable<Intake>> GetIntakesByDateAsync(string date);
        Task<IEnumerable<Intake>> GetIntakesByMedicineAndDateAsync(int medicineId, string date);
        Task<Intake?> GetIntakeByMedicineAndDateTimeAsync(int medicineId, string date, string time);
        Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync();

        // Проверка
        Task<bool> IntakeExistsAsync(int medicineId, string date, string time);

        // Удаление
        Task<int> DeleteFutureIntakesAsync(DateTime fromDate);
        Task<int> DeleteFutureIntakesForMedicineAsync(int medicineId, DateTime fromDate);
        Task<int> DeleteOldIntakesAsync(DateTime cutoffDate);
    }

    public class IntakeRepository : IIntakeRepository
    {
        private readonly AppDbContext _context;

        public IntakeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Intake?> GetIntakeByIdAsync(int intakeId)
        {
            return await _context.Intakes
                .FirstOrDefaultAsync(i => i.IdIntake == intakeId);
        }

        public async Task<IEnumerable<HistoryDto>> GetAllIntakeAsync()
        {
            return await _context.Intakes
                .Include(i => i.Medicine)
                    .ThenInclude(m => m.Unit)
                .Include(i => i.Medicine.Recipient)
                .Include(i => i.ScheduleTime)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Time)
                .Select(i => new HistoryDto
                {
                    IdIntake = i.IdIntake,
                    IdMedicine = i.IdMedicine,
                    NameMedicine = i.Medicine.Name,
                    IsCompleted = i.IsCompleted,
                    IdSchedule = i.IdSchedule,
                    IdScheduleTime = i.IdScheduleTime,
                    Date = i.Date,
                    Time = i.Time,
                    TakenDateTime = i.TakenDateTime,
                    ActualDosage = i.ActualDosage.ToString(),
                    UnitName = i.Medicine.Unit.Name,
                    IdRecipient = i.Medicine.IdRecipient,
                    RecipientName = i.Medicine.Recipient.Name ?? ""
                })
                .ToListAsync();
        }

        public async Task<int> AddIntakeAsync(Intake intake)
        {
            _context.Intakes.Add(intake);
            await _context.SaveChangesAsync();
            return intake.IdIntake;
        }

        public async Task<int> UpdateIntakeAsync(Intake intake)
        {
            var existing = await _context.Intakes.FindAsync(intake.IdIntake);
            if (existing == null) return 0;

            existing.IsCompleted = intake.IsCompleted;
            existing.TakenDateTime = intake.TakenDateTime;

            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TodayMedicineDto>> GetTodayMedicineAsync()
        {
            var today = DateTime.Now.Date.ToString("yyyy-MM-dd");

            return await _context.Intakes
                .Where(i => i.Date == today && !i.IsCompleted)
                .Include(i => i.Medicine)
                    .ThenInclude(m => m.Unit)
                .Include(i => i.Medicine.Recipient)
                .Include(i => i.ScheduleTime)
                .Include(i => i.Medicine.Stock)
                .Include(i => i.Schedule)
                .OrderBy(i => i.Medicine.Recipient.Name)
                .ThenBy(i => i.ScheduleTime.OrderInDay)
                .Select(i => new TodayMedicineDto
                {
                    IdMedicine = i.IdMedicine,
                    IdStock = i.Medicine.Stock != null ? i.Medicine.Stock.IdStock : 0,
                    CurrentQuantity = i.Medicine.Stock != null ? i.Medicine.Stock.CurrentQuantity ?? 0 : 0,
                    RecipientName = i.Medicine.Recipient.Name ?? "",
                    MedicineName = i.Medicine.Name ?? "",
                    Dosage = i.Schedule != null ? i.Schedule.Dosage : 0,
                    IdScheduleTime = i.IdScheduleTime,
                    Time = i.Time ?? "",
                    OrderInDay = i.ScheduleTime != null ? i.ScheduleTime.OrderInDay : 0,
                    UnitName = i.Medicine.Unit.Name ?? "",
                    IdSchedule = i.IdSchedule,
                    IdIntake = i.IdIntake,
                    IsCompleted = i.IsCompleted
                })
                .ToListAsync();
        }

        public async Task<Intake?> GetIntakeByMedicineAndDateAsync(int medicineId, string date)
        {
            return await _context.Intakes
                .FirstOrDefaultAsync(i => i.IdMedicine == medicineId && i.Date == date);
        }

        public async Task<IEnumerable<Intake>> GetIntakesByDateAsync(string date)
        {
            return await _context.Intakes
                .Where(i => i.Date == date)
                .OrderBy(i => i.Time)
                .ToListAsync();
        }

        public async Task<int> DeleteFutureIntakesAsync(DateTime fromDate)
        {
            var fromDateStr = fromDate.ToString("yyyy-MM-dd");
            var intakes = await _context.Intakes
                .Where(i => string.Compare(i.Date, fromDateStr) >= 0 && !i.IsCompleted)
                .ToListAsync();

            _context.Intakes.RemoveRange(intakes);
            return await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Intake>> GetIntakesByMedicineAndDateAsync(int medicineId, string date)
        {
            return await _context.Intakes
                .Where(i => i.IdMedicine == medicineId && i.Date == date)
                .OrderBy(i => i.Time)
                .ToListAsync();
        }

        public async Task<Intake?> GetIntakeByMedicineAndDateTimeAsync(int medicineId, string date, string time)
        {
            return await _context.Intakes
                .FirstOrDefaultAsync(i => i.IdMedicine == medicineId && i.Date == date && i.Time == time);
        }

        public async Task<bool> IntakeExistsAsync(int medicineId, string date, string time)
        {
            return await _context.Intakes
                .AnyAsync(i => i.IdMedicine == medicineId && i.Date == date && i.Time == time);
        }

        public async Task<int> DeleteFutureIntakesForMedicineAsync(int medicineId, DateTime fromDate)
        {
            var fromDateStr = fromDate.ToString("yyyy-MM-dd");
            var intakes = await _context.Intakes
                .Where(i => i.IdMedicine == medicineId && string.Compare(i.Date, fromDateStr) >= 0 && !i.IsCompleted)
                .ToListAsync();

            _context.Intakes.RemoveRange(intakes);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteOldIntakesAsync(DateTime cutoffDate)
        {
            var cutoffDateStr = cutoffDate.ToString("yyyy-MM-dd");
            var intakes = await _context.Intakes
                .Where(i => string.Compare(i.Date, cutoffDateStr) < 0 && i.IsCompleted)
                .ToListAsync();

            _context.Intakes.RemoveRange(intakes);
            return await _context.SaveChangesAsync();
        }
    }
}