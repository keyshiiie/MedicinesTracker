using MedicinesTracker.Modules.Medications.ViewModels;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class MedicineListPage : ContentPage
{
    private readonly MedicineListVM _viewModel;
    public MedicineListPage(MedicineListVM viewModel)
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