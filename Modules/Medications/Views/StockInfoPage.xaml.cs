

using MedicinesTracker.Modules.Medications.ViewModels;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class StockInfoPage : ContentPage
{
	public StockInfoPage(StockInfoVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel; Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            await ((StockInfoVM)BindingContext).LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}