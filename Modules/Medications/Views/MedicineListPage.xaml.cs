using MedicinesTracker.Dto;
using MedicinesTracker.Modules.Medications.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class MedicineListPage : ContentPage
{
    private readonly MedicineListVM _viewModel;

    public MedicineListPage(MedicineListVM viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnMedicineTapped(object sender, TappedEventArgs e)
    {
        try
        {
            // Получаем Border (sender)
            var border = sender as Border;
            if (border == null)
            {
                Debug.WriteLine("sender is not Border");
                return;
            }

            // Получаем BindingContext Border'а - это и есть MedicineDetailDto
            var medicine = border.BindingContext as MedicineDetailDto;

            if (medicine == null)
            {
                Debug.WriteLine("medicine is null - BindingContext is " + border.BindingContext?.GetType().Name);
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось получить данные о лекарстве", "OK");
                return;
            }

            Debug.WriteLine($"Medicine found: Id={medicine.IdMedicine}, Name={medicine.MedicineName}");

            if (medicine.IdMedicine <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", "ID лекарства не указан", "OK");
                return;
            }

            // Вызываем команду из ViewModel
            await _viewModel.OpenDetailPageCommand.ExecuteAsync(medicine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in OnMedicineTapped: {ex.Message}");
            await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось открыть страницу: {ex.Message}", "OK");
        }
    }

    private async void OnRestoreButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button?.CommandParameter is int medicineId)
        {
            if (BindingContext is MedicineListVM viewModel)
            {
                // Вызываем команду напрямую
                await viewModel.RestoreMedicineCommand.ExecuteAsync(medicineId);
            }
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is MedicineListVM vm)
        {
            vm.UpdateSearchTextCommand.Execute(e.NewTextValue);
        }
    }
}