using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Views;

public partial class StockInfoPage : ContentPage
{
	public StockInfoPage(StockInfoVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}