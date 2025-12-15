using Microsoft.Maui.Controls;

namespace MedicinesTracker.Views.Controls
{
    public partial class RecipientPicker : ContentView
    {
        public RecipientPicker()
        {
            InitializeComponent();
        }

        // BindableProperty для выбранного получателя
        public static readonly BindableProperty SelectedRecipientProperty =
            BindableProperty.Create(
                nameof(SelectedRecipient),
                typeof(object),
                typeof(RecipientPicker),
                null,
                BindingMode.TwoWay,
                propertyChanged: OnSelectedRecipientChanged);

        public object SelectedRecipient
        {
            get => GetValue(SelectedRecipientProperty);
            set => SetValue(SelectedRecipientProperty, value);
        }

        private static void OnSelectedRecipientChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var picker = (RecipientPicker)bindable;

            // Если в VM уже есть SelectedRecipient, обновляем его
            if (picker.BindingContext is ViewModels.Controls.RecipientPickerVM vm)
            {
                if (newValue is Models.RecipientModel recipient)
                {
                    if (vm.SelectedRecipient != recipient)
                    {
                        vm.SelectedRecipient = recipient;
                    }
                }
            }
        }
    }
}