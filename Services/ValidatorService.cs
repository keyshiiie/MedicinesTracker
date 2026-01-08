using MedicinesTracker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Services
{
    public interface IValidatorService
    {
        List<string> GetBaseInfoValidationErrors(
            MedicineModel? medicine,
            UnitModel? selectedUnit,
            RecipientModel? selectedRecipient,
            MethodAdmissionModel? selectedMethod);
        List<string> GetScheduleValidationErrors(
            bool isRecurring,
            bool isIntervalMode,
            bool isWeekDaysMode,
            string? dateStart,
            string? oneTimeDate,
            ScheduleTypeModel? selectedType,
            ScheduleModeModel? selectedMode,
            RecurrencePatternModel? selectedPattern,
            List<WeekDayModel> selectedDays,
            List<TimeSpan> selectedTimes);
        List<string> GetStockValidationErrors(StockModel stock);
        List<string> GetRecipientValidationErrors(RecipientModel recipient);
    }
    public class ValidatorService : IValidatorService
    {
        public List<string> GetBaseInfoValidationErrors(
            MedicineModel? medicine,
            UnitModel? selectedUnit,
            RecipientModel? selectedRecipient,
            MethodAdmissionModel? selectedMethod)
        {
            var errors = new List<string>();

            // Валидация модели лекарства
            if (medicine == null)
            {
                errors.Add("Для добавления лекарства заполните все поля.");
                return errors;
            }

            // Проверка названия
            if (string.IsNullOrWhiteSpace(medicine.Name))
                errors.Add("Укажите название лекарства.");
            else if (medicine.Name.Length > 255)
                errors.Add("Название лекарства слишком длинное. Максимум - 255 символов");

            // Проверка выбранных справочных значений
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

            // Дополнительные проверки при редактировании
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

        public List<string> GetStockValidationErrors(StockModel stock)
        {
            var errors = new List<string>();
            if(stock == null)
            {
                errors.Add("Для добавления запаса лекарства заполните все поля.");
                return errors;
            }
            if (stock.CurrentQuantity <= 0)
                errors.Add("Текущее количество лекарства должно быть больше нуля.");
            if (stock.CurrentQuantity >= 1000)
                errors.Add("Текущее количество не может быть больше 1000.");
            if (stock.Threshold <= 0)
                errors.Add("Порог напоминания должен быть больше нуля.");
            if (stock.Threshold >= 1000)
                errors.Add("Порог напоминания не может быть больше 1000.");
            return errors;
        }

        public List<string> GetScheduleValidationErrors(
            bool isRecurring,
            bool isIntervalMode,
            bool isWeekDaysMode,
            string? dateStart,
            string? oneTimeDate,
            ScheduleTypeModel? selectedType,
            ScheduleModeModel? selectedMode,
            RecurrencePatternModel? selectedPattern,
            List<WeekDayModel> selectedDays,
            List<TimeSpan> selectedTimes) // Добавляем проверку времени
        {
            var errors = new List<string>();

            if (selectedType == null)
                errors.Add("Выберите тип расписания");

            if (isRecurring)
            {
                if (selectedMode == null)
                    errors.Add("Выберите способ задания расписания");

                if (isIntervalMode && selectedPattern == null)
                    errors.Add("Выберите частоту приёма");

                if (isWeekDaysMode && !selectedDays.Any())
                    errors.Add("Выберите хотя бы один день недели");

                if (string.IsNullOrEmpty(dateStart))
                    errors.Add("Укажите дату начала приёма");
            }
            else
            {
                if (string.IsNullOrEmpty(oneTimeDate))
                    errors.Add("Укажите дату приёма");
            }

            // Проверка времени
            if (!selectedTimes.Any())
                errors.Add("Выберите хотя бы одно время приёма");

            return errors;
        }
        public List<string> GetRecipientValidationErrors(RecipientModel recipient)
        {
            var errors = new List<string>();
            if(recipient == null)
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
