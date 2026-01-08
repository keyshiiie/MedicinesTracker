using System.Globalization;

namespace MedicinesTracker.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString(@"hh\:mm");
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string timeString && TimeSpan.TryParse(timeString, out TimeSpan result))
            {
                return result;
            }
            return TimeSpan.Zero;
        }
    }
}