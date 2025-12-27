using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MedicinesTracker.Converters
{
    public class InvertedBoolToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string text)
            {
                var parts = text.Split(':');
                return boolValue ? parts[1] : parts[0];
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
