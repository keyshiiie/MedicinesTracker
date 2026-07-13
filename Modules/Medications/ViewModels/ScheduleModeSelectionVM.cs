using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using System.Diagnostics;
using MedicinesTracker.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;
using MedicinesTracker.Constants;
using static MedicinesTracker.Constants.ScheduleTypes;
using static MedicinesTracker.Constants.ScheduleModes;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(ScheduleTypeCode), "scheduleTypeCode")]
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleModeSelectionVM : CreationStepBaseVM
    {
        private readonly IReferencesDataRepository _referencesRepository;
        private readonly IMedicationCreationNavigationService _medicationNavigation;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private string _scheduleTypeCode = Recurring;

        [ObservableProperty]
        private bool _isNewMedicine = true;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private ObservableCollection<ScheduleMode> _scheduleModes = new();

        [ObservableProperty]
        private ScheduleMode? _selectedScheduleMode;

        [ObservableProperty]
        private bool _isValid = false;

        public ScheduleModeSelectionVM(
            IReferencesDataRepository referencesRepository,
            StepManager stepManager,
            IMedicationCreationNavigationService medicationNavigation,
            INavigationService navigation) : base(stepManager, navigation)
        {
            _referencesRepository = referencesRepository;
            _medicationNavigation = medicationNavigation;
            _navigation = navigation;
        }

        public override async Task ContinueAsync()
        {
            if (SelectedScheduleMode == null) return;

            if (IsNewMedicine)
            {
                _stepManager.NextStep();
            }

            await _medicationNavigation.ToScheduleDetailsAsync(
                ScheduleTypeCode,
                SelectedScheduleMode.Code,
                MedicineId,
                IsNewMedicine,
                null);
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsEditingExisting = !IsNewMedicine;

                if (IsNewMedicine && _stepManager.CurrentStep != 4)
                {
                    _stepManager.CurrentStep = 4;
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScheduleModeSelectionVM ERROR] {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось инициализировать страницу: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var modes = await _referencesRepository.GetAllScheduleModeAsync();
                ScheduleModes = new ObservableCollection<ScheduleMode>(modes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedule modes: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить способы задания: {ex.Message}");
            }
        }

        partial void OnSelectedScheduleModeChanged(ScheduleMode? value)
        {
            IsValid = value != null;
        }

        public override async Task BackAsync()
        {
            if (IsNewMedicine)
            {
                _stepManager.PreviousStep();
            }
            await _navigation.GoBackAsync();
        }
    }
}