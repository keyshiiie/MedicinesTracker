using MedicinesTracker.Modules.Notifications.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MedicinesTracker.Modules.Notifications.Views
{
    public partial class GreetingPage : ContentPage
    {
        public GreetingPage(GreetingVM viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            // Скрываем панель навигации для этой страницы
            NavigationPage.SetHasNavigationBar(this, false);
        }
    }
}