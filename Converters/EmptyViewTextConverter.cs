// Converters/EmptyViewConverters.cs
using System.Globalization;

namespace MedicinesTracker.Converters
{
    public class EmptyViewTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool)value ? "Ничего не найдено" : "Список лекарств пуст";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}