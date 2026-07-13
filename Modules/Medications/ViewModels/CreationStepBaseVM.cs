using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public abstract partial class CreationStepBaseVM : ObservableObject
    {
        protected readonly StepManager _stepManager;
        protected readonly INavigationService _navigation;

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

        protected CreationStepBaseVM(StepManager stepManager, INavigationService navigation)
        {
            _stepManager = stepManager;
            _navigation = navigation;
            _stepManager.OnStepInfoChanged += UpdateStepInfo;

            CurrentStep = _stepManager.CurrentStep;
            var stepInfo = _stepManager.GetCurrentStepInfo();
            StepTitle = stepInfo.Title;
            StepDescription = stepInfo.Description;

            ShowBackButton = CurrentStep > 1;
        }

        private void UpdateStepInfo(StepInfo stepInfo)
        {
            CurrentStep = _stepManager.CurrentStep;
            StepTitle = stepInfo.Title;
            StepDescription = stepInfo.Description;

            ShowBackButton = CurrentStep > 1 && !IsEditingExisting;
        }

        [RelayCommand]
        public virtual Task BackAsync() => _navigation.GoBackAsync();

        [RelayCommand]
        public abstract Task ContinueAsync();

        [RelayCommand]
        public virtual async Task CancelAsync()
        {
            var confirm = await _navigation.ShowConfirmationAsync(
                "Отмена создания",
                "Вы уверены, что хотите отменить создание лекарства? Все данные будут потеряны.");

            if (confirm)
            {
                _stepManager.Reset();
                await _navigation.GoToAsync("//medicines");
            }
        }

        protected async Task ShowErrorAsync(string message)
        {
            await _navigation.ShowAlertAsync("Ошибка", message);
        }

        protected async Task ShowSuccessAsync(string message)
        {
            await _navigation.ShowAlertAsync("Успех", message);
        }
    }
}