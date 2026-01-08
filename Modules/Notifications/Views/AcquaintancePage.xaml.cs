using MedicinesTracker.Modules.Notifications.ViewModels;
using Microsoft.Maui.Controls;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class AcquaintancePage : ContentPage
    {
        public AcquaintancePage(AcquaintanceVM viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            // Скрываем панель навигации для этой страницы
            NavigationPage.SetHasNavigationBar(this, false);
            viewModel.SetPage(this);  
        }
    }
}