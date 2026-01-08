using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.Services;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Notifications.Views;

public partial class TodayMedicinePage : ContentPage
{
    private readonly TodayMedicineVM _viewModel;
    private readonly IntakeSchedulerService? _schedulerService; // Делаем nullable

    public TodayMedicinePage(
        TodayMedicineVM viewModel,
        IntakeSchedulerService? schedulerService = null) // Делаем nullable с default
    {
        InitializeComponent();
        _viewModel = viewModel;
        _schedulerService = schedulerService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("[TodayMedicinePage] OnAppearing - проверка и обновление записей");

            // Проверяем и обновляем записи о приеме (если сервис доступен)
            if (_schedulerService != null)
            {
                await _schedulerService.CheckAndUpdateAsync();
            }

            // Загружаем данные лекарств на сегодня
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
            await DisplayAlertAsync("Ошибка", "Не удалось загрузить данные", "ОК");
        }
    }
}