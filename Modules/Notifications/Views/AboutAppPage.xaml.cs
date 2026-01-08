using MedicinesTracker.Modules.Notifications.ViewModels;
using Microsoft.Maui.Controls;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class AboutAppPage : ContentPage
    {
        public AboutAppPage(AboutAppVM viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            // Скрываем панель навигации для этой страницы
            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}