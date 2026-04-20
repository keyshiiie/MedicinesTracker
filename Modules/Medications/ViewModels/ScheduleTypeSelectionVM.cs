using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using MedicinesTracker.Modules.Medications.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Entities;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;
using MedicinesTracker.Constants;
using static MedicinesTracker.Constants.ScheduleTypes;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleTypeSelectionVM : CreationStepBaseVM
    {
        private readonly IReferencesDataRepository _referencesRepository;
        private readonly IMedicationCreationNavigationService _medicationNavigation;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private ObservableCollection<ScheduleType> _scheduleTypes = new();

        [ObservableProperty]
        private ScheduleType? _selectedScheduleType;

        [ObservableProperty]
        private bool _isValid = false;

        [ObservableProperty]
        private bool _isNewMedicine = true;

        [ObservableProperty]
        private int _medicineId;

        public ScheduleTypeSelectionVM(
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
            if (SelectedScheduleType == null) return;

            if (SelectedScheduleType.Code == Recurring)
            {
                if (IsNewMedicine)
                {
                    _stepManager.NextStep();
                }
                await _medicationNavigation.ToScheduleModeSelectionAsync(
                    SelectedScheduleType.Code,
                    MedicineId,
                    IsNewMedicine);
            }
            else if (SelectedScheduleType.Code == OneTime)
            {
                if (IsNewMedicine)
                {
                    _stepManager.CurrentStep = 5;
                }
                await _medicationNavigation.ToScheduleDetailsAsync(
                    SelectedScheduleType.Code,
                    null,
                    MedicineId,
                    IsNewMedicine,
                    null);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsEditingExisting = !IsNewMedicine;

                if (IsNewMedicine && _stepManager.CurrentStep != 3)
                {
                    _stepManager.CurrentStep = 3;
                }

                Debug.WriteLine($"ScheduleTypeSelectionVM InitializeAsync - IsNewMedicine: {IsNewMedicine}, MedicineId: {MedicineId}");

                if (!IsNewMedicine && MedicineId <= 0)
                {
                    Debug.WriteLine($"Warning: Для редактирования лекарства MedicineId должен быть > 0");
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScheduleTypeSelectionVM ERROR] {ex.Message}\nStackTrace: {ex.StackTrace}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var types = await _referencesRepository.GetAllScheduleTypeAsync();
                ScheduleTypes = new ObservableCollection<ScheduleType>(types);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedule types: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить типы расписаний: {ex.Message}");
            }
        }

        partial void OnSelectedScheduleTypeChanged(ScheduleType? value)
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