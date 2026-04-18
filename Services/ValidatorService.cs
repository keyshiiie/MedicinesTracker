using MedicinesTracker.Entities;
namespace MedicinesTracker.Services
{
    public interface IValidatorService
    {
        List<string> GetBaseInfoValidationErrors(
            Medicine? medicine,
            Unit? selectedUnit,
            Recipient? selectedRecipient,
            MethodAdmission? selectedMethod);
        List<string> GetScheduleValidationErrors(
            bool isRecurring,
            bool isIntervalMode,
            bool isWeekDaysMode,
            string? dateStart,
            string? dateEnd,
            string? oneTimeDate,
            int dosage,
            ScheduleType? selectedType,
            ScheduleMode? selectedMode,
            RecurrencePattern? selectedPattern,
            List<WeekDay> selectedDays,
            List<TimeSpan> selectedTimes);
        List<string> GetStockValidationErrors(Stock stock);
        List<string> GetRecipientValidationErrors(Recipient recipient);
    }

    public class ValidatorService : IValidatorService
    {
        public List<string> GetBaseInfoValidationErrors(
            Medicine? medicine,
            Unit? selectedUnit,
            Recipient? selectedRecipient,
            MethodAdmission? selectedMethod)
        {
            var errors = new List<string>();

            if (medicine == null)
            {
                errors.Add("Для добавления лекарства заполните все поля.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(medicine.Name))
                errors.Add("Укажите название лекарства.");
            else if (medicine.Name.Length > 255)
                errors.Add("Название лекарства слишком длинное. Максимум - 255 символов");

            if (selectedUnit == null)
                errors.Add("Выберите единицу измерения.");
            else if (selectedUnit.IdUnit <= 0)
                errors.Add("Выберите корректную единицу измерения.");

            if (selectedRecipient == null)
                errors.Add("Выберите получателя.");
            else if (selectedRecipient.IdRecipient <= 0)
                errors.Add("Выберите корректного получателя.");

            if (selectedMethod == null)
                errors.Add("Выберите способ приёма.");
            else if (selectedMethod.IdMethodAdmission <= 0)
                errors.Add("Выберите корректный способ приёма.");

            if (medicine.IdMedicine > 0)
            {
                if (medicine.IdUnit <= 0)
                    errors.Add("ID единицы измерения не указан.");

                if (medicine.IdRecipient <= 0)
                    errors.Add("ID получателя не указан.");

                if (medicine.IdMethodAdmission <= 0)
                    errors.Add("ID способа приёма не указан.");
            }

            return errors;
        }

        public List<string> GetStockValidationErrors(Stock stock)
        {
            var errors = new List<string>();

            if (stock == null)
            {
                errors.Add("Для добавления запаса лекарства заполните все поля.");
                return errors;
            }

            if (!stock.CurrentQuantity.HasValue)
            {
                errors.Add("Укажите текущее количество лекарства.");
            }
            else if (stock.CurrentQuantity.Value < 0)
            {
                errors.Add("Текущее количество лекарства не может быть отрицательным.");
            }
            else if (stock.CurrentQuantity.Value == 0)
            {
                errors.Add("Текущее количество лекарства должно быть больше нуля.");
            }
            else if (stock.CurrentQuantity.Value > 1000)
            {
                errors.Add("Текущее количество не может быть больше 1000.");
            }

            if (!stock.Threshold.HasValue)
            {
                errors.Add("Укажите порог напоминания.");
            }
            else if (stock.Threshold.Value < 0)
            {
                errors.Add("Порог напоминания не может быть отрицательным.");
            }
            else if (stock.Threshold.Value == 0)
            {
                errors.Add("Порог напоминания должен быть больше нуля.");
            }
            else if (stock.Threshold.Value > 1000)
            {
                errors.Add("Порог напоминания не может быть больше 1000.");
            }

            if (stock.CurrentQuantity.HasValue && stock.Threshold.HasValue)
            {
                if (stock.Threshold.Value > stock.CurrentQuantity.Value)
                {
                    errors.Add("Порог напоминания не может быть больше текущего количества.");
                }
            }

            return errors;
        }

        public List<string> GetScheduleValidationErrors(
            bool isRecurring,
            bool isIntervalMode,
            bool isWeekDaysMode,
            string? dateStart,
            string? dateEnd,
            string? oneTimeDate,
            int dosage,
            ScheduleType? selectedType,
            ScheduleMode? selectedMode,
            RecurrencePattern? selectedPattern,
            List<WeekDay> selectedDays,
            List<TimeSpan> selectedTimes)
        {
            var errors = new List<string>();

            if (dosage < 1)
                errors.Add("Дозировка должна быть больше нуля");
            else if (dosage > 100)
                errors.Add("Дозировка не может быть больше 100");

            if (isRecurring)
            {
                if (selectedMode == null)
                {
                    errors.Add("Выберите способ задания расписания");
                }
                else
                {
                    if (isIntervalMode && selectedPattern == null)
                        errors.Add("Выберите частоту приёма");

                    if (isWeekDaysMode && !selectedDays.Any())
                        errors.Add("Выберите хотя бы один день недели");
                }

                if (string.IsNullOrEmpty(dateStart))
                    errors.Add("Укажите дату начала приёма");

                if (!string.IsNullOrEmpty(dateStart) && !string.IsNullOrEmpty(dateEnd))
                {
                    if (DateTime.TryParse(dateStart, out var startDate) &&
                        DateTime.TryParse(dateEnd, out var endDate))
                    {
                        if (endDate < startDate)
                            errors.Add("Дата окончания приёма не может быть раньше даты начала");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(oneTimeDate))
                    errors.Add("Укажите дату приёма");
            }

            if (!selectedTimes.Any())
                errors.Add("Выберите хотя бы одно время приёма");

            return errors;
        }

        public List<string> GetRecipientValidationErrors(Recipient recipient)
        {
            var errors = new List<string>();

            if (recipient == null)
            {
                errors.Add("Для добавления получателя заполните все поля.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(recipient.Name))
                errors.Add("Укажите имя получателя.");
            else if (recipient.Name.Length > 255)
                errors.Add("Имя получателя слишком длинное. Максимум - 255 символов");

            return errors;
        }
    }
}