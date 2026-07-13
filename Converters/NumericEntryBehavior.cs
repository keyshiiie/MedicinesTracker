using System;
using System.Collections.Generic;
using System.Text;

namespace MedicinesTracker.Converters
{
    public class NumericEntryBehavior : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;

            // Если текст пустой или null - оставляем пустым
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                return;
            }

            // Проверяем, что введено число
            if (!int.TryParse(e.NewTextValue, out _))
            {
                entry.Text = e.OldTextValue;
            }
        }
    }
}
