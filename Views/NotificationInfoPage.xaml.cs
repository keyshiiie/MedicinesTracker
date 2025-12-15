using MedicinesTracker.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Views;

public partial class NotificationInfoPage : ContentPage
{
	public NotificationInfoPage(NotificationInfoVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            await ((NotificationInfoVM)BindingContext).InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}