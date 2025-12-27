using MedicinesTracker.Modules.Settings.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Settings.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsPageVM viewModels)
	{
		InitializeComponent();
		BindingContext = viewModels; 
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            await ((SettingsPageVM)BindingContext).LoadData();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}