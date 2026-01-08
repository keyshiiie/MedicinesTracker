using MedicinesTracker.Modules.Medications.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class MedicineListPage : ContentPage
{
    public MedicineListPage(MedicineListVM viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[MedicineListPage] OnAppearing - обновление данных");
            await ((MedicineListVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}