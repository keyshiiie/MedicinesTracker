using MedicinesTracker.Modules.Medications.ViewModels;

namespace MedicinesTracker.Modules.Medications.Views;

public partial class MedicineDetailPage : ContentPage
{
    public MedicineDetailPage(MedicineDetailVM viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

