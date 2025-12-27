using MedicinesTracker.Modules.Notifications.ViewModels;
using MedicinesTracker.ViewModels;

namespace MedicinesTracker.Modules.Notifications.Views;

public partial class TodayMedicineListPage : ContentPage
{
	public TodayMedicineListPage(TodayMedicineVM viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}