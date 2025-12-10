using MedicinesTracker.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Views;

public partial class MedicineListPage : ContentPage
{
    public MedicineListPage(MedicineListVM viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

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
            await DisplayAlert("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}