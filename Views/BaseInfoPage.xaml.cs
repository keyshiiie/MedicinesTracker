using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Views;

public partial class BaseInfoPage : ContentPage
{
	public BaseInfoPage(BaseInfoVM viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}