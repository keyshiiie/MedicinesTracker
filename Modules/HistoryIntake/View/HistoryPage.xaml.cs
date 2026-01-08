using MedicinesTracker.Modules.HistoryIntake.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.HistoryIntake.View;

public partial class HistoryPage : ContentPage
{
	public HistoryPage(HistoryPageVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[HistoryPage] OnAppearing - обновление данных");
            await ((HistoryPageVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}