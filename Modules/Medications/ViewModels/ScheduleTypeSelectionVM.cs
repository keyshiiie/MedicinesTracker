// ViewModels/ScheduleTypeSelectionVM.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Repository;
using MedicinesTracker.Modules.Medications.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Models;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class ScheduleTypeSelectionVM : ObservableObject
    {
        private readonly IReferencesDataRepository _referencesRepository;

        [ObservableProperty]
        private ObservableCollection<ScheduleTypeModel> _scheduleTypes = new();

        [ObservableProperty]
        private ScheduleTypeModel? _selectedScheduleType;

        [ObservableProperty]
        private bool _isValid = false;

        [ObservableProperty]
        private bool _isNewMedicine = true;

        [ObservableProperty]
        private int _medicineId;

        public ScheduleTypeSelectionVM(IReferencesDataRepository referencesRepository)
        {
            _referencesRepository = referencesRepository;
            LoadDataAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
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

        [RelayCommand]
        private async Task ContinueAsync()
        {
            if (SelectedScheduleType == null) return;

            Debug.WriteLine($"ScheduleTypeSelectionVM ContinueAsync - Type: {SelectedScheduleType.Code}, IsNewMedicine: {IsNewMedicine}, MedicineId: {MedicineId}");

            var parameters = new Dictionary<string, object>
            {
                { "scheduleTypeCode", SelectedScheduleType.Code },
                { "isNewMedicine", IsNewMedicine },
                { "medicineId", MedicineId }
            };

            // Для нового лекарства MedicineId будет 0, это нормально
            if (SelectedScheduleType.Code == "RECURRING")
            {
                // Переходим к выбору способа задания расписания
                Debug.WriteLine($"Переходим к ScheduleModeSelectionPage");
                await Shell.Current.GoToAsync("ScheduleModeSelectionPage", parameters);
            }
            else if (SelectedScheduleType.Code == "ONETIME")
            {
                // Переходим к заполнению одноразового расписания
                Debug.WriteLine($"Переходим к ScheduleDetailsPage");
                await Shell.Current.GoToAsync("ScheduleDetailsPage", parameters);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var types = await _referencesRepository.GetAllScheduleTypeAsync();
                ScheduleTypes = new ObservableCollection<ScheduleTypeModel>(types);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedule types: {ex.Message}");
            }
        }

        partial void OnSelectedScheduleTypeChanged(ScheduleTypeModel? value)
        {
            IsValid = value != null;
        }

        [RelayCommand]
        private async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}