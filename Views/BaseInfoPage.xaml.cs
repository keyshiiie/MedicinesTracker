using MedicinesTracker.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Views;

public partial class BaseInfoPage : ContentPage
{
	public BaseInfoPage(BaseInfoVM viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel; Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            await ((BaseInfoVM)BindingContext).LoadData();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}