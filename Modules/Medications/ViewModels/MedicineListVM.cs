using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public partial class MedicineListVM : ObservableObject
    {
        private readonly IMedicineRepository _medicineRepository;

        [ObservableProperty]
        private ObservableCollection<MedicineDetailDto> _medicineDetails = new();

        [ObservableProperty]
        private RecipientModel? _selectedRecipient;


        public MedicineListVM(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
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
            if (medicine.IdMedicine <= 0)
            {
                Debug.WriteLine("Ошибка: IdMedicine должен быть > 0");
                return;
            }
            try
            {
                var route = "MedicineDetailPage";
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", medicine.IdMedicine },
                    { "idStock", medicine.IdStock },
                    { "unitName", medicine.UnitName ?? string.Empty},
                    { "idSchedule", medicine.IdSchedule }
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
                var rawData = await _medicineRepository.GetMedicineDetailsAsync();
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

        [RelayCommand]
        public async Task AddMedicine()
        {
            try
            {
                var route = "BaseInfoPage";
                // Для добавления нового лекарства передаем idMedicine = 0
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", 0 }
                };
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу добавления: {ex.Message}");
            }
        }
    }
}