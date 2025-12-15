using MedicinesTracker.Services;
using MedicinesTracker.ViewModels;
using MedicinesTracker.ViewModels.Controls;
using System.Diagnostics;

namespace MedicinesTracker.Views;

public partial class MedicineListPage : ContentPage
{
    public MedicineListPage(MedicineListVM viewModel, MedicineService medicineService)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Создаем VM для пикера
        var recipientPickerVM = new RecipientPickerVM(medicineService);
        recipientPicker.BindingContext = recipientPickerVM;

        // Подписываемся на изменение SelectedRecipient в пикере
        recipientPickerVM.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(RecipientPickerVM.SelectedRecipient))
            {
                viewModel.SelectedRecipient = recipientPickerVM.SelectedRecipient;
            }
        };

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            await ((MedicineListVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}