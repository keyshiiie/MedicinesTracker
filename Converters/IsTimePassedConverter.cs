using System.Globalization;

namespace MedicinesTracker.Converters
{
    public class IsTimePassedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string timeString && !string.IsNullOrEmpty(timeString))
            {
                if (TimeSpan.TryParse(timeString, out var medicineTime))
                {
                    return medicineTime < DateTime.Now.TimeOfDay;
                }
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}