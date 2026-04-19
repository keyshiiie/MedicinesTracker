using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using MedicinesTracker.Modules.Medications.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Entities;
using MedicinesTracker.Services;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleTypeSelectionVM : CreationStepBaseVM
    {
        private readonly IReferencesDataRepository _referencesRepository;

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

        public ScheduleTypeSelectionVM(IReferencesDataRepository referencesRepository,
            StepManager stepManager) : base(stepManager)
        {
            _referencesRepository = referencesRepository;
        }

        public override async Task ContinueAsync()
        {
            if (SelectedScheduleType == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "scheduleTypeCode", SelectedScheduleType.Code },
                { "isNewMedicine", IsNewMedicine },
                { "medicineId", MedicineId }
            };

            if (SelectedScheduleType.Code == "RECURRING")
            {
                if (IsNewMedicine)
                {
                    _stepManager.NextStep();  // Переход к шагу 4
                }
                await Shell.Current.GoToAsync("ScheduleModeSelectionPage", parameters);
            }
            else if (SelectedScheduleType.Code == "ONETIME")
            {
                if (IsNewMedicine)
                {
                    _stepManager.CurrentStep = 5;  // Сразу к шагу 5
                }
                await Shell.Current.GoToAsync("ScheduleDetailsPage", parameters);
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Устанавливаем признак редактирования
                IsEditingExisting = !IsNewMedicine;

                // Для нового лекарства убеждаемся, что шаг правильный
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
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось загрузить данные: {ex.Message}", "OK");
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
                _stepManager.PreviousStep();  // Возврат к шагу 2
            }
            await Shell.Current.GoToAsync("..");
        }
    }
}