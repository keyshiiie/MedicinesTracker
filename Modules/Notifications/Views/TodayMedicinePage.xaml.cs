using MedicinesTracker.Modules.Notifications.ViewModels;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class TodayMedicinePage : ContentPage
    {
        public TodayMedicinePage(TodayMedicineVM viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is TodayMedicineVM viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}