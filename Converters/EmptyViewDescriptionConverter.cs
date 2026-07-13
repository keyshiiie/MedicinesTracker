using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MedicinesTracker.Converters
{
    public class EmptyViewDescriptionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool)value
                ? "Попробуйте изменить поисковый запрос"
                : "Вы можете добавить новое лекарство при помощи кнопки ниже.";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
