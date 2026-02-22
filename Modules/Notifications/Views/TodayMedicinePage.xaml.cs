using MedicinesTracker.Modules.Notifications.ViewModels;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class TodayMedicinePage : ContentPage
    {
        private TodayMedicineVM _viewModel;
        public TodayMedicinePage(TodayMedicineVM viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}