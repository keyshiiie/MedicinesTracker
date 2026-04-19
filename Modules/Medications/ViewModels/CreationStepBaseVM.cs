using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Services;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public abstract partial class CreationStepBaseVM : ObservableObject
    {
        protected readonly StepManager _stepManager;

        [ObservableProperty]
        private int _currentStep;

        [ObservableProperty]
        private int _totalSteps = 5;

        [ObservableProperty]
        private string _stepTitle = string.Empty;

        [ObservableProperty]
        private string _stepDescription = string.Empty;

        [ObservableProperty]
        private bool _isValid = false;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private bool _showBackButton;

        [ObservableProperty]
        private bool _isEditingExisting; 

        protected CreationStepBaseVM(StepManager stepManager)
        {
            _stepManager = stepManager;
            _stepManager.OnStepInfoChanged += UpdateStepInfo;

            CurrentStep = _stepManager.CurrentStep;
            var stepInfo = _stepManager.GetCurrentStepInfo();
            StepTitle = stepInfo.Title;
            StepDescription = stepInfo.Description;

            // Показываем кнопку Назад на всех страницах кроме первой
            ShowBackButton = CurrentStep > 1;
        }

        private void UpdateStepInfo(StepInfo stepInfo)
        {
            CurrentStep = _stepManager.CurrentStep;
            StepTitle = stepInfo.Title;
            StepDescription = stepInfo.Description;

            // Обновляем видимость кнопки Назад
            ShowBackButton = CurrentStep > 1 && !IsEditingExisting;
        }

        [RelayCommand]
        public virtual Task BackAsync() => Shell.Current.GoToAsync("..");

        [RelayCommand]
        public abstract Task ContinueAsync();

        [RelayCommand]
        public virtual async Task CancelAsync()
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                "Отмена создания",
                "Вы уверены, что хотите отменить создание лекарства? Все данные будут потеряны.",
                "Да", "Нет");

            if (confirm)
            {
                _stepManager.Reset();
                await Shell.Current.GoToAsync("//medicines");
            }
        }

        protected async Task ShowErrorAsync(string message)
        {
            await Shell.Current.DisplayAlertAsync("Ошибка", message, "OK");
        }

        protected async Task ShowSuccessAsync(string message)
        {
            await Shell.Current.DisplayAlertAsync("Успех", message, "OK");
        }
    }
}