
using MedicinesTracker.Modules.Medications.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class MedicineSchedulePage : ContentPage
{
	public MedicineSchedulePage(MedicineScheduleVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[BaseInfoPagePage] OnAppearing - обновление данных");
            await ((MedicineScheduleVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}