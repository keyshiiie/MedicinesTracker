using MedicinesTracker.Modules.Settings.ViewModels;

namespace MedicinesTracker.Modules.Settings.Views;

public partial class EditRecipientPage : ContentPage
{
	public EditRecipientPage(EditRecipientVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}