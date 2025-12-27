using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MedicinesTracker.Converters
{
    public class StringToDateConverter : IValueConverter
    {
        // string → DateTime (для DatePicker)
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string dateString && !string.IsNullOrEmpty(dateString))
            {
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
            }
            return DateTime.Today;
        }

        // DateTime → string (из DatePicker в DTO)
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("yyyy-MM-dd");
            }
            return string.Empty;
        }
    }
}