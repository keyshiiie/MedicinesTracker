using MedicinesTracker.Modules.Medications.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class ScheduleDetailsPage : ContentPage
{
	public ScheduleDetailsPage(ScheduleDetailsVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[ScheduleDetailsPage] OnAppearing - обновление данных");
            await ((ScheduleDetailsVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}