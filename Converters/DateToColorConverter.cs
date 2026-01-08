using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MedicinesTracker.Converters
{
    public class DateToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value as string))
                return Colors.Gray; // Серый цвет для пустой даты
            return Colors.Black;    // Черный цвет для выбранной даты
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
