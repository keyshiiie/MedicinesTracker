using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using System.Diagnostics;
using MedicinesTracker.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MedicinesTracker.Services;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(ScheduleTypeCode), "scheduleTypeCode")]
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleModeSelectionVM : CreationStepBaseVM
    {
        private readonly IReferencesDataRepository _referencesRepository;

        [ObservableProperty]
        private string _scheduleTypeCode = "RECURRING";

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

        public ScheduleModeSelectionVM(IReferencesDataRepository referencesRepository,
            StepManager stepManager) : base(stepManager)
        {
            _referencesRepository = referencesRepository;
        }

        public override async Task ContinueAsync()
        {
            if (SelectedScheduleMode == null) return;

            if (IsNewMedicine)
            {
                _stepManager.NextStep();  // Переход к шагу 5
            }

            var parameters = new Dictionary<string, object>
            {
                { "scheduleTypeCode", ScheduleTypeCode },
                { "scheduleModeCode", SelectedScheduleMode.Code },
                { "isRecurring", true },
                { "isNewMedicine", IsNewMedicine },
                { "medicineId", MedicineId }
            };

            await Shell.Current.GoToAsync("ScheduleDetailsPage", parameters);
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Устанавливаем признак редактирования
                IsEditingExisting = !IsNewMedicine;

                // Для нового лекарства убеждаемся, что шаг правильный
                if (IsNewMedicine && _stepManager.CurrentStep != 4)
                {
                    _stepManager.CurrentStep = 4;
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScheduleModeSelectionVM ERROR] {ex.Message}");
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
                _stepManager.PreviousStep();  // Возврат к шагу 3
            }
            await Shell.Current.GoToAsync("..");
        }
    }
}