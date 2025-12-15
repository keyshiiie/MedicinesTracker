using MedicinesTracker.ViewModels;
using System.Diagnostics;

namespace MedicinesTracker.Views;

public partial class MedicineDetailPage : ContentPage
{
    public MedicineDetailPage(MedicineDetailVM viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

