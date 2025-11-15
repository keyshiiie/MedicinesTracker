using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Views;

public partial class NotificationInfoPage : ContentPage
{
	public NotificationInfoPage(NotificationInfoVM viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}