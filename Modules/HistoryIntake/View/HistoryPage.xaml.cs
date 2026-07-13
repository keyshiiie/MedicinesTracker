using MedicinesTracker.Modules.HistoryIntake.ViewModels;

namespace MedicinesTracker.Modules.HistoryIntake.View;

public partial class HistoryPage : ContentPage
{
    private HistoryPageVM _viewModel;
	public HistoryPage(HistoryPageVM viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}