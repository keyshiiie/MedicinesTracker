using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public abstract partial class BaseCreationStepVM : ObservableObject
    {
        [ObservableProperty]
        private int _currentStep;

        [ObservableProperty]
        private int _totalSteps = 4; // BaseInfo, Stock, ScheduleType, ScheduleDetails

        [ObservableProperty]
        private string _stepTitle = string.Empty;

        public abstract string Title { get; }
        public abstract string Description { get; }

        [RelayCommand]
        public abstract Task ContinueAsync();

        [RelayCommand]
        public abstract Task BackAsync();

        [RelayCommand]
        public virtual async Task CancelAsync()
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                "Отмена создания",
                "Вы уверены, что хотите отменить создание лекарства? Все данные будут потеряны.",
                "Да", "Нет");
            
            if (confirm)
            {
                await Shell.Current.GoToAsync("//medicines");
            }
        }

        protected string GetStepTitle()
        {
            return CurrentStep switch
            {
                1 => "Основная информация",
                2 => "Запас",
                3 => "Тип расписания",
                4 => "Детали расписания",
                _ => "Шаг " + CurrentStep
            };
        }
    }
}