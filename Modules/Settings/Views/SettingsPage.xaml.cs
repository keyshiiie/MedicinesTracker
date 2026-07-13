using MedicinesTracker.Modules.Settings.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Settings.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsPageVM viewModels)
	{
		InitializeComponent();
		BindingContext = viewModels;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[BaseInfoPagePage] OnAppearing - обновление данных");
            await ((SettingsPageVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}