using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.ViewModels
{
    public partial class MedicineListVM : ObservableObject
    {
        private readonly MedicineService _medicineService;

        [ObservableProperty]
        private ObservableCollection<MedicineDetailDto> _medicineDetails = new();

        [ObservableProperty]
        private RecipientModel? _selectedRecipient;

        [ObservableProperty]
        private ObservableCollection<MedicineDetailDto> _filteredMedicineDetails = new();

        public bool HasMedicines => FilteredMedicineDetails.Any();

        public MedicineListVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        // Обновляем фильтрованный список при изменении получателя или данных
        partial void OnSelectedRecipientChanged(RecipientModel? value)
        {
            UpdateFilteredList();
        }

        partial void OnMedicineDetailsChanged(ObservableCollection<MedicineDetailDto> value)
        {
            UpdateFilteredList();
        }

        private void UpdateFilteredList()
        {
            if (SelectedRecipient == null)
            {
                FilteredMedicineDetails = new ObservableCollection<MedicineDetailDto>();
                return;
            }

            var filtered = MedicineDetails
                .Where(m => m.RecipientName == SelectedRecipient.Name)
                .ToList();

            FilteredMedicineDetails = new ObservableCollection<MedicineDetailDto>(filtered);

            Debug.WriteLine($"[MedicineListVM] FilteredMedicineDetails обновлен: {FilteredMedicineDetails.Count} элементов");
        }

        public async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("[MedicineListVM] InitializeAsync начат");
                await LoadDataAsync();
                Debug.WriteLine("[MedicineListVM] InitializeAsync завершен");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MedicineListVM ERROR] {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task OpenDetailPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;
            try
            {
                var route = "MedicineDetailPage";
                var parameters = new Dictionary<string, object>
                {
                    { "medicine", medicine }
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу детализации: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var rawData = await _medicineService.GetAllMedicineDetailsAsync();
                MedicineDetails = new ObservableCollection<MedicineDetailDto>(rawData);

                Debug.WriteLine($"[MedicineListVM] Загружено {MedicineDetails.Count} лекарств");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task RefreshData()
        {
            await LoadDataAsync();
        }
    }
}