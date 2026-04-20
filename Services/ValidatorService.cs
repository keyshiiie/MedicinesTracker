using MedicinesTracker.Constants;
using MedicinesTracker.Entities;
using System.Collections.ObjectModel;

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
        (bool IsValid, string ErrorMessage, int? Value) ValidatePositiveInt(string text, string fieldName, int maxValue = 1000);
        (bool IsValid, string ErrorMessage) ValidateRequiredString(string? value, string fieldName, int maxLength = 255);
        (bool IsValid, string ErrorMessage) ValidateRequiredSelection<T>(T? selectedItem, string fieldName);
        (bool IsValid, string ErrorMessage) ValidateTimeSelection(List<TimeSpan> selectedTimes);
        (bool IsValid, string ErrorMessage) ValidateWeekDaysSelection(List<WeekDay> selectedDays);
        (bool IsValid, string ErrorMessage) ValidateDateRange(string? startDate, string? endDate);
        (bool IsValid, string ErrorMessage, int Value) ValidateDosageField(string text);
        (bool IsValid, string ErrorMessage) ValidateTimesField(ObservableCollection<TimeSpan> selectedTimes);
        (bool IsValid, string ErrorMessage) ValidateWeekDaysField(List<WeekDay> weekDays, bool isWeekDaysMode);
        (bool IsValid, string ErrorMessage) ValidateRecurrencePatternField(RecurrencePattern? pattern, bool isIntervalMode);
        (bool IsValid, string ErrorMessage) ValidateDatesField(bool isOneTime, string? oneTimeDate, string? dateStart, string? dateEnd);
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
            else if (medicine.Name.Length > ValidationConstants.MaxMedicineNameLength)
                errors.Add($"Название лекарства слишком длинное. Максимум - {ValidationConstants.MaxMedicineNameLength} символов");

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
            else if (stock.CurrentQuantity.Value > ValidationConstants.MaxQuantity)
            {
                errors.Add($"Текущее количество не может быть больше {ValidationConstants.MaxQuantity}.");
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
            else if (stock.Threshold.Value > ValidationConstants.MaxQuantity)
            {
                errors.Add($"Порог напоминания не может быть больше {ValidationConstants.MaxQuantity}.");
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

            if (dosage < ValidationConstants.MinDosage)
                errors.Add($"Дозировка должна быть больше {ValidationConstants.MinDosage}");
            else if (dosage > ValidationConstants.MaxDosage)
                errors.Add($"Дозировка не может быть больше {ValidationConstants.MaxDosage}");

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
            else if (recipient.Name.Length > ValidationConstants.MaxRecipientNameLength)
                errors.Add($"Имя получателя слишком длинное. Максимум - {ValidationConstants.MaxRecipientNameLength} символов");

            return errors;
        }

        public (bool IsValid, string ErrorMessage, int? Value) ValidatePositiveInt(
            string text,
            string fieldName,
            int maxValue = ValidationConstants.MaxQuantity)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, $"Введите {fieldName.ToLower()}", null);
            }

            if (!int.TryParse(text, out int value))
            {
                return (false, "Введите целое число", null);
            }

            if (value < 0)
            {
                return (false, $"{fieldName} не может быть отрицательным", null);
            }

            if (value == 0)
            {
                return (false, $"{fieldName} должен быть больше нуля", null);
            }

            if (value > maxValue)
            {
                return (false, $"{fieldName} не может быть больше {maxValue}", null);
            }

            return (true, string.Empty, value);
        }

        public (bool IsValid, string ErrorMessage) ValidateRequiredString(
            string? value,
            string fieldName,
            int maxLength = ValidationConstants.MaxMedicineNameLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return (false, $"Укажите {fieldName.ToLower()}");
            }

            if (value.Length > maxLength)
            {
                return (false, $"{fieldName} слишком длинный. Максимум - {maxLength} символов");
            }

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateRequiredSelection<T>(
            T? selectedItem,
            string fieldName)
        {
            if (selectedItem == null)
            {
                return (false, $"Выберите {fieldName.ToLower()}");
            }

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateTimeSelection(List<TimeSpan> selectedTimes)
        {
            if (selectedTimes == null || !selectedTimes.Any())
            {
                return (false, "Выберите хотя бы одно время приёма");
            }

            if (selectedTimes.Count > ValidationConstants.MaxTimesCount)
            {
                return (false, $"Нельзя добавить более {ValidationConstants.MaxTimesCount} времен приёма");
            }

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateWeekDaysSelection(List<WeekDay> selectedDays)
        {
            if (selectedDays == null || !selectedDays.Any(d => d.IsSelected))
            {
                return (false, "Выберите хотя бы один день недели");
            }

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateDateRange(string? startDate, string? endDate)
        {
            if (string.IsNullOrEmpty(startDate))
            {
                return (false, "Укажите дату начала приёма");
            }

            if (string.IsNullOrEmpty(endDate))
            {
                return (false, "Укажите дату окончания приёма");
            }

            if (DateTime.TryParse(startDate, out var start) &&
                DateTime.TryParse(endDate, out var end))
            {
                if (end < start)
                {
                    return (false, "Дата окончания приёма не может быть раньше даты начала");
                }

                if (start < DateTime.Today)
                {
                    return (false, "Дата начала не может быть в прошлом");
                }
            }
            else
            {
                return (false, "Неверный формат даты");
            }

            return (true, string.Empty);
        }

        // Валидация дозировки
        public (bool IsValid, string ErrorMessage, int Value) ValidateDosageField(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (false, "Введите дозировку", 0);
            }

            if (!int.TryParse(text, out int dosage))
            {
                return (false, "Введите целое число", 0);
            }

            if (dosage < ValidationConstants.MinDosage)
            {
                return (false, $"Дозировка должна быть больше {ValidationConstants.MinDosage}", dosage);
            }

            if (dosage > ValidationConstants.MaxDosage)
            {
                return (false, $"Дозировка не может быть больше {ValidationConstants.MaxDosage}", dosage);
            }

            return (true, string.Empty, dosage);
        }

        // Валидация времени приёма
        public (bool IsValid, string ErrorMessage) ValidateTimesField(ObservableCollection<TimeSpan> selectedTimes)
        {
            if (selectedTimes == null || selectedTimes.Count == 0)
            {
                return (false, "Выберите хотя бы одно время приёма");
            }

            if (selectedTimes.Count > ValidationConstants.MaxTimesCount)
            {
                return (false, $"Нельзя добавить более {ValidationConstants.MaxTimesCount} времен приёма");
            }

            return (true, string.Empty);
        }

        // Валидация выбранных дней недели
        public (bool IsValid, string ErrorMessage) ValidateWeekDaysField(List<WeekDay> weekDays, bool isWeekDaysMode)
        {
            if (!isWeekDaysMode) return (true, string.Empty);

            var selectedDays = weekDays?.Where(d => d.IsSelected).ToList() ?? new List<WeekDay>();

            if (selectedDays.Count == 0)
            {
                return (false, "Выберите хотя бы один день недели");
            }

            return (true, string.Empty);
        }

        // Валидация выбранной частоты приёма
        public (bool IsValid, string ErrorMessage) ValidateRecurrencePatternField(RecurrencePattern? pattern, bool isIntervalMode)
        {
            if (!isIntervalMode) return (true, string.Empty);

            if (pattern == null)
            {
                return (false, "Выберите частоту приёма");
            }

            return (true, string.Empty);
        }

        // Валидация дат
        public (bool IsValid, string ErrorMessage) ValidateDatesField(bool isOneTime, string? oneTimeDate, string? dateStart, string? dateEnd)
        {
            if (isOneTime)
            {
                if (string.IsNullOrEmpty(oneTimeDate))
                {
                    return (false, "Укажите дату приёма");
                }

                if (DateTime.TryParse(oneTimeDate, out var date))
                {
                    if (date < DateTime.Today)
                    {
                        return (false, "Дата приёма не может быть в прошлом");
                    }
                }
                else
                {
                    return (false, "Неверный формат даты");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(dateStart))
                {
                    return (false, "Укажите дату начала приёма");
                }

                if (string.IsNullOrEmpty(dateEnd))
                {
                    return (false, "Укажите дату окончания приёма");
                }

                if (DateTime.TryParse(dateStart, out var startDate) &&
                    DateTime.TryParse(dateEnd, out var endDate))
                {
                    if (endDate < startDate)
                    {
                        return (false, "Дата окончания приёма не может быть раньше даты начала");
                    }

                    if (startDate < DateTime.Today)
                    {
                        return (false, "Дата начала не может быть в прошлом");
                    }
                }
                else
                {
                    return (false, "Неверный формат даты");
                }
            }

            return (true, string.Empty);
        }
    }
}