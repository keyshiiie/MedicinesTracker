using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.HistoryIntake.ViewModels
{
    public partial class HistoryPageVM : ObservableObject
    {
        private readonly IIntakeRepository _intakeRepository;
        private List<HistoryDto> _allIntakes = new();

        [ObservableProperty]
        private ObservableCollection<GroupedHistory> _intakes = new();

        [ObservableProperty]
        private ObservableCollection<GroupedHistory> _filteredIntakes = new();

        [ObservableProperty]
        private ObservableCollection<RecipientFilter> _recipients = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFiltered))]
        private RecipientFilter? _selectedRecipient; // Добавлено "?"

        [ObservableProperty]
        private bool _isLoading;

        public bool IsFiltered => SelectedRecipient != null;

        [ObservableProperty]
        private bool _hasData;

        [ObservableProperty]
        private string _emptyMessage = "Нет данных о приеме лекарств";

        public HistoryPageVM(IIntakeRepository intakeRepository)
        {
            _intakeRepository = intakeRepository;

            // Подписываемся на изменение SelectedRecipient
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) // Добавлено "?"
        {
            if (e.PropertyName == nameof(SelectedRecipient))
            {
                // При изменении выбранного получателя автоматически применяем фильтр
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
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var data = await _intakeRepository.GetAllIntakeAsync();
                _allIntakes = data?.ToList() ?? new List<HistoryDto>(); // Обработка возможного null

                // Обновляем список получателей
                UpdateRecipientsList(_allIntakes);

                // Загружаем данные без фильтра
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
            SelectedRecipient = default!;
        }

        private void ApplyFilter()
        {
            try
            {
                // Фильтруем данные
                var filteredData = SelectedRecipient != null
                    ? _allIntakes.Where(m => m.RecipientName == SelectedRecipient.Name).ToList()
                    : _allIntakes.ToList();

                // Группируем по дате
                var grouped = filteredData
                    .GroupBy(m => m.Date)
                    .Select(g => new GroupedHistory(
                        g.Key ?? string.Empty, // Добавлена проверка на null
                        g.OrderByDescending(m => m.Time)  // Внутри даты сортируем по времени
                    ))
                    .OrderByDescending(g => g.Date)  // Даты сортируем по убыванию
                    .ToList();

                // Обновляем отображаемые данные
                FilteredIntakes.Clear();

                foreach (var group in grouped)
                {
                    FilteredIntakes.Add(group);
                }

                // Обновляем статус наличия данных
                HasData = FilteredIntakes.Any() && FilteredIntakes.Any(g => g.Medicines.Any());

                // Обновляем сообщение при отсутствии данных
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

        private void UpdateRecipientsList(List<HistoryDto> intakes)
        {
            // Получаем уникальных получателей из данных
            var uniqueRecipients = intakes
                .Select(m => m.RecipientName)
                .Where(name => !string.IsNullOrEmpty(name)) // Фильтрация null/пустых значений
                .Distinct()
                .OrderBy(name => name)
                .Select(name => new RecipientFilter(name))
                .ToList();

            // Обновляем ObservableCollection
            Recipients.Clear();
            foreach (var recipient in uniqueRecipients)
            {
                Recipients.Add(recipient);
            }

            // Сбрасываем выбор (показываем всех)
            SelectedRecipient = default!;
        }
    }
}