using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    public partial class MedicineListVM : ObservableObject
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly INotificationPlannerService _notificationPlanner;
        private readonly IIntakeGeneratorService _intakeGenerator;

        [ObservableProperty]
        private ObservableCollection<MedicineDetailDto> _medicineDetails = new();

        [ObservableProperty]
        private ObservableCollection<MedicineDetailDto> _filteredMedicineDetails = new();

        [ObservableProperty]
        private Recipient? _selectedRecipient;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private bool _isActiveSelected = true;  // По умолчанию показываем активные

        [ObservableProperty]
        private bool _isArchiveSelected;

        [ObservableProperty]
        private bool _showAddButton = true;  // Показываем кнопку "Добавить"

        [ObservableProperty]
        private string _pageTitle = "Лекарства";

        [ObservableProperty]
        private string _emptyViewTitle = "Список лекарств пуст";

        [ObservableProperty]
        private string _emptyViewDescription = "Нажмите на кнопку +, чтобы добавить первое лекарство";

        // Свойства для кнопки переключения
        [ObservableProperty]
        private string _toggleButtonIcon = "archive_icon.png";

        public MedicineListVM(IMedicineRepository medicineRepository,
            INotificationPlannerService notificationPlanner,
            IIntakeGeneratorService intakeGenerator)
        {
            _medicineRepository = medicineRepository;
            _notificationPlanner = notificationPlanner;
            _intakeGenerator = intakeGenerator;
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
        private async Task ToggleMode()
        {
            if (IsActiveSelected)
            {
                // Переключаемся в архив
                await ShowArchivedMedicines();
            }
            else
            {
                // Переключаемся в активные
                await ShowActiveMedicines();
            }
        }

        [RelayCommand]
        private async Task ShowActiveMedicines()
        {
            if (IsActiveSelected) return;

            IsActiveSelected = true;
            IsArchiveSelected = false;
            ShowAddButton = true;
            PageTitle = "Лекарства";

            // Обновляем кнопку
            ToggleButtonIcon = "archive_icon.png";

            // Обновляем тексты для пустого состояния
            EmptyViewTitle = "Список лекарств пуст";
            EmptyViewDescription = "Нажмите на кнопку +, чтобы добавить первое лекарство";

            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ShowArchivedMedicines()
        {
            if (IsArchiveSelected) return;

            IsArchiveSelected = true;
            IsActiveSelected = false;
            ShowAddButton = false;
            PageTitle = "Архив";

            // Обновляем кнопку
            ToggleButtonIcon = "medicine_box.png";

            // Обновляем тексты для пустого состояния
            EmptyViewTitle = "Архив пуст";
            EmptyViewDescription = "Здесь будут лекарства, которые вы отправили в архив";

            await LoadDataAsync();
        }

        [RelayCommand]
        private void UpdateSearchText(string newText)
        {
            SearchText = newText ?? string.Empty;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredMedicineDetails = new ObservableCollection<MedicineDetailDto>(MedicineDetails);
            }
            else
            {
                var filtered = MedicineDetails
                    .Where(m => m.MedicineName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
                FilteredMedicineDetails = new ObservableCollection<MedicineDetailDto>(filtered);
            }

            IsSearching = !string.IsNullOrWhiteSpace(SearchText);
        }

        [RelayCommand]
        private async Task OpenDetailPage(MedicineDetailDto medicine)
        {
            if (medicine is null) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "idMedicine", medicine.IdMedicine },
                    { "medicineName", medicine.MedicineName ?? string.Empty},
                    { "idStock", medicine.IdStock },
                    { "unitName", medicine.UnitName ?? string.Empty},
                    { "idSchedule", medicine.IdSchedule },
                    { "isArchived", medicine.IsArchived }
                };
                await Shell.Current.GoToAsync("MedicineDetailPage", parameters);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось открыть страницу: {ex.Message}", "OK");
            }
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;

                IEnumerable<MedicineDetailDto> rawData;

                if (IsActiveSelected)
                {
                    rawData = await _medicineRepository.GetMedicineDetailsAsync();
                    // Помечаем, что это не архивные записи
                    foreach (var item in rawData)
                    {
                        item.IsArchived = false;
                    }
                }
                else
                {
                    rawData = await _medicineRepository.GetArchivedMedicinesAsync();
                    // Помечаем, что это архивные записи
                    foreach (var item in rawData)
                    {
                        item.IsArchived = true;
                    }
                }

                MedicineDetails = new ObservableCollection<MedicineDetailDto>(rawData);
                FilteredMedicineDetails = new ObservableCollection<MedicineDetailDto>(rawData);
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
                IsRefreshing = false;
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
                IsRefreshing = false;
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

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            ApplyFilter();
        }

        [RelayCommand]
        private async Task RestoreMedicine(int medicineId)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Восстановление",
                "Восстановить лекарство из архива?\n\nИстория приёмов сохранится.",
                "Да",
                "Нет");

            if (!confirm) return;

            try
            {
                var success = await _medicineRepository.RestoreMedicineAsync(medicineId);

                if (success)
                {
                    await _intakeGenerator.RegenerateIntakesForMedicineAsync(medicineId);

                    // Перепланируем уведомления
                    _notificationPlanner.CancelAll();
                    await _notificationPlanner.PlanForTodayAsync();

                    await Shell.Current.DisplayAlertAsync("Успех", "Лекарство восстановлено", "ОК");

                    // Если мы в архиве и восстановили лекарство — переключаемся на активные
                    if (IsArchiveSelected)
                    {
                        await ShowActiveMedicines();
                    }
                    else
                    {
                        await LoadDataAsync();
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось восстановить лекарство", "ОК");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при восстановлении: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось восстановить: {ex.Message}", "ОК");
            }
        }
    }
}