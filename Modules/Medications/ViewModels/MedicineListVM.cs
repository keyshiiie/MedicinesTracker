using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
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
        private Recipient? _selectedRecipient;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _isLoading;

        public MedicineListVM(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
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
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", medicine.IdMedicine },
                    { "medicineName", medicine.MedicineName ?? string.Empty},
                    { "idStock", medicine.IdStock },
                    { "unitName", medicine.UnitName ?? string.Empty},
                    { "idSchedule", medicine.IdSchedule }
                };
                await Shell.Current.GoToAsync("MedicineDetailPage", parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу детализации: {ex.Message}");
            }
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return; // Предотвращаем множественные загрузки

            try
            {
                _isLoading = true;

                var rawData = await _medicineRepository.GetMedicineDetailsAsync();
                MedicineDetails = new ObservableCollection<MedicineDetailDto>(rawData);

                Debug.WriteLine($"[MedicineListVM] Загружено {MedicineDetails.Count} лекарств");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    "Не удалось загрузить список лекарств. Попробуйте позже.",
                    "OK");
            }
            finally
            {
                _isLoading = false;
                IsRefreshing = false; // ВАЖНО: всегда сбрасываем IsRefreshing
            }
        }

        [RelayCommand]
        public async Task RefreshData()
        {
            try
            {
                IsRefreshing = true;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении: {ex.Message}");
                IsRefreshing = false; // Сбрасываем даже при ошибке
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    "Не удалось обновить список. Попробуйте позже.",
                    "OK");
            }
        }

        [RelayCommand]
        public async Task AddMedicine()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", 0 }
                };

                await Shell.Current.GoToAsync("BaseInfoPage", parameters);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось открыть страницу: {ex.Message}", "OK");
            }
        }
    }
}