using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Views;

public partial class TodayMedicineListPage : ContentPage
{
	public TodayMedicineListPage(TodayMedicineVM viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}