using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.HistoryIntake.ViewModels
{
    public partial class HistoryPageVM : ObservableObject
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IRecipientRepository _recipientRepository;
        private List<HistoryDto> _allIntakes = new();

        [ObservableProperty]
        private ObservableCollection<GroupedHistory> _filteredIntakes = new();

        [ObservableProperty]
        private ObservableCollection<RecipientFilter> _recipients = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFiltered))]
        private RecipientFilter? _selectedRecipient;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isRefreshing;

        public bool IsFiltered => SelectedRecipient != null;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private string _emptyMessage = "Нет данных о приеме лекарств";

        public HistoryPageVM(
            IIntakeRepository intakeRepository,
            IRecipientRepository recipientRepository)
        {
            _intakeRepository = intakeRepository;
            _recipientRepository = recipientRepository;
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedRecipient))
            {
                ApplyFilter();
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HistoryPageVM ERROR] {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
            IsRefreshing = false;
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                Debug.WriteLine("=== Загрузка истории ===");

                // 1. Загружаем ВСЕХ получателей
                var recipients = await _recipientRepository.GetAllRecipientsAsync();
                UpdateRecipientsList(recipients);

                Debug.WriteLine($"Загружено получателей: {Recipients.Count}");

                // 2. Загружаем записи приема
                var data = await _intakeRepository.GetAllIntakeAsync();
                _allIntakes = data?.ToList() ?? new List<HistoryDto>();

                Debug.WriteLine($"Загружено записей: {_allIntakes.Count}");

                // 3. Применяем фильтр
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки истории: {ex.Message}");
                EmptyMessage = "Ошибка загрузки данных";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearFilter()
        {
            SelectedRecipient = null;
        }

        private void ApplyFilter()
        {
            try
            {
                Debug.WriteLine($"Применяем фильтр. Выбран: {SelectedRecipient?.Name ?? "Все"}");

                // Фильтруем данные
                var filteredData = SelectedRecipient != null
                    ? _allIntakes.Where(m => m.RecipientName == SelectedRecipient.Name).ToList()
                    : _allIntakes.ToList();

                Debug.WriteLine($"После фильтрации: {filteredData.Count} записей");

                // Группируем по дате
                var grouped = filteredData
                    .GroupBy(m => m.Date)
                    .Select(g => new GroupedHistory(
                        g.Key ?? string.Empty,
                        g.OrderByDescending(m => m.Time)
                    ))
                    .OrderByDescending(g => g.Date)
                    .ToList();

                Debug.WriteLine($"Сформировано групп: {grouped.Count}");

                FilteredIntakes.Clear();
                foreach (var group in grouped)
                {
                    FilteredIntakes.Add(group);
                    Debug.WriteLine($"  - Группа {group.Date}: {group.Medicines.Count} записей");
                }

                HasData = FilteredIntakes.Any() && FilteredIntakes.Any(g => g.Medicines.Any());
                Debug.WriteLine($"HasData: {HasData}");

                if (!HasData)
                {
                    EmptyMessage = SelectedRecipient != null
                        ? $"Нет данных о приеме лекарств для {SelectedRecipient.Name}"
                        : "Нет данных о приеме лекарств";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка фильтрации: {ex.Message}");
            }
        }

        private void UpdateRecipientsList(IEnumerable<Recipient> recipients)
        {
            Debug.WriteLine("Обновляем список получателей из БД");

            var recipientFilters = recipients
                .OrderBy(r => r.Name)
                .Select(r => new RecipientFilter(r.Name))
                .ToList();

            Debug.WriteLine($"Получателей в БД: {recipientFilters.Count}");

            // Сохраняем текущий выбор
            var currentSelection = SelectedRecipient?.Name;

            // Обновляем ObservableCollection
            Recipients.Clear();
            foreach (var recipient in recipientFilters)
            {
                Recipients.Add(recipient);
                Debug.WriteLine($"  - Добавлен: {recipient.Name}");
            }

            // Восстанавливаем выбор, если получатель все еще существует в списке
            if (!string.IsNullOrEmpty(currentSelection))
            {
                SelectedRecipient = Recipients.FirstOrDefault(r => r.Name == currentSelection);
                Debug.WriteLine($"Восстановлен выбор: {SelectedRecipient?.Name}");
            }
            else
            {
                SelectedRecipient = null;
                Debug.WriteLine("Выбор сброшен");
            }
        }
    }
}