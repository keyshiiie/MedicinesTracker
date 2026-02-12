// ViewModels/ScheduleModeSelectionVM.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using MedicinesTracker.Modules.Medications.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Models;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(ScheduleTypeCode), "scheduleTypeCode")]
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleModeSelectionVM : ObservableObject
    {
        private readonly IReferencesDataRepository _referencesRepository;

        [ObservableProperty]
        private string _scheduleTypeCode = "RECURRING";

        [ObservableProperty]
        private bool _isNewMedicine = true;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private ObservableCollection<ScheduleModeModel> _scheduleModes = new();

        [ObservableProperty]
        private ScheduleModeModel? _selectedScheduleMode;

        [ObservableProperty]
        private bool _isValid = false;

        public ScheduleModeSelectionVM(IReferencesDataRepository referencesRepository)
        {
            _referencesRepository = referencesRepository;
            LoadDataAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MedicineScheduleVM ERROR] {ex.Message}");
            }
        }
        private async Task LoadDataAsync()
        {
            try
            {
                var modes = await _referencesRepository.GetAllScheduleModeAsync();
                ScheduleModes = new ObservableCollection<ScheduleModeModel>(modes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedule modes: {ex.Message}");
            }
        }

        partial void OnSelectedScheduleModeChanged(ScheduleModeModel? value)
        {
            IsValid = value != null;
        }

        [RelayCommand]
        private async Task ContinueAsync()
        {
            if (SelectedScheduleMode == null) return;

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

        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}