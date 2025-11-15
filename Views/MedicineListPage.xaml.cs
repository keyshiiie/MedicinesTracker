using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Views;

public partial class MedicineListPage : ContentPage
{
    public MedicineListPage(MedicineListVM viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}