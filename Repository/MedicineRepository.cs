using MedicinesTracker.Data;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IMedicineRepository
    {
        Task<IEnumerable<MedicineDetailDto>> GetMedicineDetailsAsync();
        Task<int> UpdateMedicineAsync(Medicine medicine);
        Task<int> DeleteMedicineAsync(int idMedicine);
        Task<int> AddMedicineAsync(Medicine medicine);
        Task<MedicineDetailDto?> GetMedicineDetailByIdAsync(int idMedicine);
        Task<Medicine?> GetMedicineByIdAsync(int idMedicine);
        Task<IEnumerable<MedicineWithScheduleDto>> GetActiveMedicinesWithSchedulesAsync();
        Task<MedicineWithScheduleDto?> GetMedicineWithScheduleByIdAsync(int medicineId);
    }

    public class MedicineRepository : IMedicineRepository
    {
        private readonly AppDbContext _context;

        public MedicineRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MedicineDetailDto>> GetMedicineDetailsAsync()
        {
            return await _context.Medicines
                .Include(m => m.Stock)
                .Include(m => m.MethodAdmission)
                .Include(m => m.Unit)
                .Include(m => m.Recipient)
                .Include(m => m.Schedules)
                .Select(m => new MedicineDetailDto
                {
                    IdMedicine = m.IdMedicine,
                    IdSchedule = m.Schedules.FirstOrDefault() != null ? m.Schedules.First().IdSchedule : 0,
                    MedicineName = m.Name,
                    MethodAdmissionName = m.MethodAdmission.Name,
                    UnitName = m.Unit.Name,
                    IdStock = m.Stock != null ? m.Stock.IdStock : 0,
                    CurrentQuantity = m.Stock != null ? m.Stock.CurrentQuantity ?? 0 : 0,
                    Threshold = m.Stock != null ? m.Stock.Threshold ?? 0 : 0,
                    ReminderEnabled = m.Stock != null ? m.Stock.ReminderEnabled : false,
                    IdRecipient = m.IdRecipient,
                    RecipientName = m.Recipient.Name
                })
                .ToListAsync();
        }

        public async Task<MedicineDetailDto?> GetMedicineDetailByIdAsync(int idMedicine)
        {
            return await _context.Medicines
                .Include(m => m.Stock)
                .Include(m => m.MethodAdmission)
                .Include(m => m.Unit)
                .Include(m => m.Recipient)
                .Where(m => m.IdMedicine == idMedicine)
                .Select(m => new MedicineDetailDto
                {
                    IdMedicine = m.IdMedicine,
                    MedicineName = m.Name,
                    MethodAdmissionName = m.MethodAdmission.Name,
                    UnitName = m.Unit.Name,
                    IdStock = m.Stock != null ? m.Stock.IdStock : 0,
                    CurrentQuantity = m.Stock != null ? m.Stock.CurrentQuantity ?? 0 : 0,
                    Threshold = m.Stock != null ? m.Stock.Threshold ?? 0 : 0,
                    ReminderEnabled = m.Stock != null ? m.Stock.ReminderEnabled : false,
                    IdRecipient = m.IdRecipient,
                    RecipientName = m.Recipient.Name
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Medicine?> GetMedicineByIdAsync(int idMedicine)
        {
            return await _context.Medicines
                .FirstOrDefaultAsync(m => m.IdMedicine == idMedicine);
        }

        public async Task<int> UpdateMedicineAsync(Medicine medicine)
        {
            var existing = await _context.Medicines.FindAsync(medicine.IdMedicine);
            if (existing == null) return 0;

            existing.Name = medicine.Name;
            existing.IdUnit = medicine.IdUnit;
            existing.IdMethodAdmission = medicine.IdMethodAdmission;
            existing.IdRecipient = medicine.IdRecipient;
            existing.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteMedicineAsync(int idMedicine)
        {
            var medicine = await _context.Medicines
                .Include(m => m.Stock)
                .Include(m => m.Schedules)
                .FirstOrDefaultAsync(m => m.IdMedicine == idMedicine);

            if (medicine == null) return 0;

            _context.Medicines.Remove(medicine);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> AddMedicineAsync(Medicine medicine)
        {
            medicine.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();
            return medicine.IdMedicine;
        }

        public async Task<IEnumerable<MedicineWithScheduleDto>> GetActiveMedicinesWithSchedulesAsync()
        {
            var result = await _context.MedicationSchedules
                .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Recipient)
                .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Unit)
                .Include(ms => ms.ScheduleType)
                .Include(ms => ms.ScheduleMode)
                .Include(ms => ms.RecurrencePattern)
                .Include(ms => ms.ScheduleWeekDays)
                    .ThenInclude(swd => swd.WeekDay)
                .Include(ms => ms.ScheduleTimes)
                .Where(ms => ms.IsActive)
                .Select(ms => new MedicineWithScheduleDto
                {
                    IdMedicine = ms.IdMedicine,
                    MedicineName = ms.Medicine.Name,
                    RecipientName = ms.Medicine.Recipient.Name,
                    UnitName = ms.Medicine.Unit.Name,
                    IdUnit = ms.Medicine.IdUnit,
                    IdRecipient = ms.Medicine.IdRecipient,
                    IdSchedule = ms.IdSchedule,
                    IdScheduleType = ms.IdScheduleType,
                    ScheduleTypeCode = ms.ScheduleType.Code,
                    ScheduleTypeName = ms.ScheduleType.Name,
                    IdScheduleMode = ms.IdScheduleMode,
                    ScheduleModeCode = ms.ScheduleMode != null ? ms.ScheduleMode.Code : null,
                    ScheduleModeName = ms.ScheduleMode != null ? ms.ScheduleMode.Name : null,
                    IdRecurrencePattern = ms.IdRecurrencePattern,
                    DaysInterval = ms.RecurrencePattern != null ? ms.RecurrencePattern.DaysInterval : null,
                    RecurrencePatternName = ms.RecurrencePattern != null ? ms.RecurrencePattern.Name : null,
                    OneTimeDate = ms.OneTimeDate,
                    Dosage = ms.Dosage,
                    DateStart = ms.DateStart,
                    DateEnd = ms.DateEnd,
                    ScheduleIsActive = ms.IsActive,
                    WeekDayIds = string.Join(",", ms.ScheduleWeekDays.Select(swd => swd.IdDay)),
                    WeekDays = string.Join(",", ms.ScheduleWeekDays.Select(swd => swd.WeekDay.Name)),
                    Times = string.Join(",", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.Time)),
                    TimeOrders = string.Join(",", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.OrderInDay))
                })
                .ToListAsync();

            return result;
        }

        public async Task<MedicineWithScheduleDto?> GetMedicineWithScheduleByIdAsync(int medicineId)
        {
            return await _context.MedicationSchedules
                .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Recipient)
                .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Unit)
                .Include(ms => ms.ScheduleType)
                .Include(ms => ms.ScheduleMode)
                .Include(ms => ms.RecurrencePattern)
                .Include(ms => ms.ScheduleWeekDays)
                    .ThenInclude(swd => swd.WeekDay)
                .Include(ms => ms.ScheduleTimes)
                .Where(ms => ms.IdMedicine == medicineId && ms.IsActive)
                .Select(ms => new MedicineWithScheduleDto
                {
                    IdMedicine = ms.IdMedicine,
                    MedicineName = ms.Medicine.Name,
                    RecipientName = ms.Medicine.Recipient.Name,
                    UnitName = ms.Medicine.Unit.Name,
                    IdUnit = ms.Medicine.IdUnit,
                    IdRecipient = ms.Medicine.IdRecipient,
                    IdSchedule = ms.IdSchedule,
                    IdScheduleType = ms.IdScheduleType,
                    ScheduleTypeCode = ms.ScheduleType.Code,
                    ScheduleTypeName = ms.ScheduleType.Name,
                    IdScheduleMode = ms.IdScheduleMode,
                    ScheduleModeCode = ms.ScheduleMode != null ? ms.ScheduleMode.Code : null,
                    ScheduleModeName = ms.ScheduleMode != null ? ms.ScheduleMode.Name : null,
                    IdRecurrencePattern = ms.IdRecurrencePattern,
                    DaysInterval = ms.RecurrencePattern != null ? ms.RecurrencePattern.DaysInterval : null,
                    RecurrencePatternName = ms.RecurrencePattern != null ? ms.RecurrencePattern.Name : null,
                    OneTimeDate = ms.OneTimeDate,
                    Dosage = ms.Dosage,
                    DateStart = ms.DateStart,
                    DateEnd = ms.DateEnd,
                    ScheduleIsActive = ms.IsActive,
                    WeekDayIds = string.Join(",", ms.ScheduleWeekDays.Select(swd => swd.IdDay)),
                    WeekDays = string.Join(",", ms.ScheduleWeekDays.Select(swd => swd.WeekDay.Name)),
                    Times = string.Join(",", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.Time)),
                    TimeOrders = string.Join(",", ms.ScheduleTimes.Where(st => st.IsActive).OrderBy(st => st.OrderInDay).Select(st => st.OrderInDay))
                })
                .FirstOrDefaultAsync();
        }
    }
}