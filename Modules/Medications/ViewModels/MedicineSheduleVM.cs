using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(ScheduleId), "idSchedule")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class MedicineScheduleVM : ObservableObject
    {
        private readonly IScheduleService _scheduleService;
        private readonly IReferencesDataRepository _referencesDateRepository;

        [ObservableProperty]
        private ScheduleUIState _uiState = new();

        private readonly ScheduleFieldCleaner _fieldCleaner = new();
        private string? _previousScheduleTypeCode;
        private string? _previousScheduleModeCode;

        private readonly IValidatorService _validator;

        [ObservableProperty]
        private MedicineScheduleDto _medicineSchedule = new();

        [ObservableProperty]
        private int _scheduleId;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private string? _unitName;

        [ObservableProperty]
        private ObservableCollection<ScheduleTypeModel> _scheduleTypes = new();

        [ObservableProperty]
        private ScheduleTypeModel? _selectedScheduleType;

        [ObservableProperty]
        private ObservableCollection<ScheduleModeModel> _scheduleModes = new();

        [ObservableProperty]
        private ScheduleModeModel? _selectedScheduleMode;

        [ObservableProperty]
        private ObservableCollection<WeekDayModel> _weekDays = new();

        [ObservableProperty]
        private ObservableCollection<RecurrencePatternModel> _recurrencePatterns = new();

        [ObservableProperty]
        private RecurrencePatternModel? _selectedRecurrencePattern;

        [ObservableProperty]
        private bool _isEditingExisting;

        private bool _isInitialized = false;

        [ObservableProperty]
        private bool _canEditSchedule = true;

        // Новые свойства для отображения выбранных дней
        [ObservableProperty]
        private ObservableCollection<TimeSpan> _selectedTimes = new();

        [ObservableProperty]
        private TimeSpan _newTime = TimeSpan.FromHours(8);

        [ObservableProperty]
        private string _selectedDaysText = "Не выбрано";

        [ObservableProperty]
        private bool _hasSelectedDays;

        [ObservableProperty]
        private string _selectedTimesText = "Не выбрано";

        public MedicineScheduleVM(IScheduleService sheduleService,
            IReferencesDataRepository referencesDateRepository,
            IValidatorService validatorService)
        {
            _scheduleService = sheduleService;
            _referencesDateRepository = referencesDateRepository;
            _validator = validatorService;
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

        // Обработка изменения MedicineId
        partial void OnScheduleIdChanged(int value)
        {
            IsEditingExisting = value > 0;
            CanEditSchedule = !IsEditingExisting;

            Debug.WriteLine($"ScheduleId изменен: {value}, IsEditingExisting: {IsEditingExisting}");

            if (value >= 0 && !_isInitialized)
            {
                _isInitialized = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        partial void OnMedicineIdChanged(int value)
        {
            Debug.WriteLine($"MedicineId изменен: {value}");

            if (value > 0 && !_isInitialized)
            {
                _isInitialized = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        partial void OnSelectedRecurrencePatternChanged(RecurrencePatternModel? value)
        {
            if (value != null)
            {
                MedicineSchedule.IdRecurrencePattern = value.IdPattern;
                Debug.WriteLine($"SelectedRecurrencePattern изменен: {value.Name} (ID: {value.IdPattern})");
            }
            else
            {
                MedicineSchedule.IdRecurrencePattern = null;
                Debug.WriteLine($"SelectedRecurrencePattern сброшен");
            }
        }

        partial void OnSelectedScheduleModeChanged(ScheduleModeModel? value)
        {
            if (value != null)
            {
                // Обновляем IdScheduleMode в MedicineSchedule
                MedicineSchedule.IdScheduleMode = value.IdMode;
                Debug.WriteLine($"SelectedScheduleMode изменен: {value.Name} (ID: {value.IdMode})");

                // Получаем локальные копии для передачи в метод
                var weekDays = WeekDays;
                var recurrencePattern = _selectedRecurrencePattern;

                _fieldCleaner.CleanForScheduleMode(
                    value.Code,
                    _previousScheduleModeCode,
                    ref weekDays,
                    ref recurrencePattern);

                // Обновляем свойства
                if (recurrencePattern != _selectedRecurrencePattern)
                {
                    SelectedRecurrencePattern = recurrencePattern;
                }

                UiState.UpdateForScheduleMode(value.Code);
                _previousScheduleModeCode = value.Code;

                // Сбрасываем выбранные дни при смене режима
                ResetSelectedDays();
            }
            else
            {
                // Если режим сброшен, тоже сбрасываем Id
                MedicineSchedule.IdScheduleMode = null;
                Debug.WriteLine($"SelectedScheduleMode сброшен");

                UiState.UpdateForScheduleMode(null);
                _previousScheduleModeCode = null;
            }
        }

        partial void OnSelectedScheduleTypeChanged(ScheduleTypeModel? value)
        {
            if (value != null)
            {
                // Обновляем IdScheduleType в MedicineSchedule
                MedicineSchedule.IdScheduleType = value.IdType;
                Debug.WriteLine($"SelectedScheduleType изменен: {value.Name} (ID: {value.IdType})");

                // Получаем локальные копии для передачи в метод
                var scheduleMode = _selectedScheduleMode;
                var recurrencePattern = _selectedRecurrencePattern;
                var weekDays = WeekDays;
                var medicineSchedule = MedicineSchedule;

                _fieldCleaner.CleanForScheduleType(
                    value.Code,
                    _previousScheduleTypeCode,
                    ref medicineSchedule,
                    ref scheduleMode,
                    ref recurrencePattern,
                    ref weekDays);

                // Обновляем свойства
                if (medicineSchedule != MedicineSchedule)
                {
                    MedicineSchedule = medicineSchedule;
                }
                if (scheduleMode != _selectedScheduleMode)
                {
                    SelectedScheduleMode = scheduleMode;
                }
                if (recurrencePattern != _selectedRecurrencePattern)
                {
                    SelectedRecurrencePattern = recurrencePattern;
                }

                UiState.UpdateForScheduleType(value.Code);
                _previousScheduleTypeCode = value.Code;

                // Сбрасываем выбранные дни при смене типа
                ResetSelectedDays();
            }
            else
            {
                // Если тип сброшен, тоже сбрасываем Id
                MedicineSchedule.IdScheduleType = 0;
                Debug.WriteLine($"SelectedScheduleType сброшен");

                UiState.UpdateForScheduleType(null);
                _previousScheduleTypeCode = null;
            }
        }

        // Команда для добавления времени
        [RelayCommand]
        private void AddTime()
        {
            if (!SelectedTimes.Contains(NewTime))
            {
                SelectedTimes.Add(NewTime);
                SelectedTimes = new ObservableCollection<TimeSpan>(
                    SelectedTimes.OrderBy(t => t));
                UpdateTimesText();
            }
        }

        // Команда для удаления времени
        [RelayCommand]
        private void RemoveTime(TimeSpan time)
        {
            SelectedTimes.Remove(time);
            UpdateTimesText();
        }

        private void UpdateTimesText()
        {
            SelectedTimesText = SelectedTimes.Any()
                ? string.Join(", ", SelectedTimes.Select(t => t.ToString(@"hh\:mm")))
                : "Не выбрано";
        }


        // Метод для сброса выбранных дней
        private void ResetSelectedDays()
        {
            if (WeekDays != null)
            {
                foreach (var day in WeekDays)
                {
                    day.IsSelected = false;
                }
                UpdateSelectedDaysText();
            }
        }

        // Обновление текста выбранных дней
        private void UpdateSelectedDaysText()
        {
            if (WeekDays == null) return;

            var selectedDays = WeekDays.Where(d => d.IsSelected).ToList();
            HasSelectedDays = selectedDays.Any();

            if (HasSelectedDays)
            {
                SelectedDaysText = string.Join(", ", selectedDays.Select(d => d.Name));
                // Обновляем строку с днями в MedicineSchedule
                MedicineSchedule.WeekDays = string.Join(",", selectedDays.Select(d => d.Name));
            }
            else
            {
                SelectedDaysText = "Не выбрано";
                MedicineSchedule.WeekDays = string.Empty;
            }
        }

        // Обработчик изменения состояния чекбоксов
        private void OnWeekDaySelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WeekDayModel.IsSelected))
            {
                UpdateSelectedDaysText();
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                Debug.WriteLine($"LoadData: MedicineId={MedicineId}, ScheduleId={ScheduleId}, IsEditingExisting={IsEditingExisting}");

                if (!ScheduleTypes.Any() || !WeekDays.Any() || !RecurrencePatterns.Any())
                {
                    await LoadReferenceData();
                }

                if (IsEditingExisting && ScheduleId > 0)
                {
                    // Редактирование: загружаем по ScheduleId
                    Debug.WriteLine($"Загружаем данные для редактирования (ScheduleId={ScheduleId})");
                    await LoadMedicineDataAsync(ScheduleId);
                }
                else if (MedicineId > 0)
                {
                    // Добавление: MedicineId должен быть передан
                    Debug.WriteLine($"Добавление нового расписания для MedicineId={MedicineId}");
                    ResetForm();

                    // Убедимся, что MedicineId установлен в DTO
                    MedicineSchedule.IdMedicine = MedicineId;
                }
                else
                {
                    Debug.WriteLine("Ошибка: не указан ни MedicineId, ни ScheduleId");
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Не указано лекарство для создания расписания", "OK");
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось загрузить данные: {ex.Message}", "OK");
            }
        }

        private async Task LoadMedicineDataAsync(int medicineId)
        {
            try
            {
                Debug.WriteLine($"Загружаем данные расписания по ScheduleId={ScheduleId}");

                var schedule = await _scheduleService.GetScheduleByIdAsync(ScheduleId);

                if (schedule != null)
                {
                    MedicineSchedule = schedule;

                    // Убедимся, что MedicineId тоже установлен
                    MedicineId = schedule.IdMedicine;

                    Debug.WriteLine($"Загружено: MedicineId={schedule.IdMedicine}, ScheduleTypeId={schedule.IdScheduleType}");

                    if (ScheduleTypes.Any())
                    {
                        // Находим тип расписания по ID
                        SelectedScheduleType = ScheduleTypes
                            .FirstOrDefault(st => st.IdType == schedule.IdScheduleType);

                        if (SelectedScheduleType == null && !string.IsNullOrEmpty(schedule.ScheduleTypeName))
                        {
                            SelectedScheduleType = ScheduleTypes
                                .FirstOrDefault(st => st.Name == schedule.ScheduleTypeName);
                        }

                        if (SelectedScheduleType != null)
                        {
                            MedicineSchedule.IdScheduleType = SelectedScheduleType.IdType;
                        }
                    }

                    if (UiState.IsRecurringSchedule && ScheduleModes.Any() &&
                        !string.IsNullOrEmpty(MedicineSchedule.ScheduleModeCode))
                    {
                        SelectedScheduleMode = ScheduleModes
                            .FirstOrDefault(sm => sm.IdMode == MedicineSchedule.IdScheduleMode);
                    }

                    if (UiState.IsIntervalMode && RecurrencePatterns.Any() &&
                        MedicineSchedule.IdRecurrencePattern.HasValue)
                    {
                        SelectedRecurrencePattern = RecurrencePatterns
                            .FirstOrDefault(rp => rp.IdPattern == MedicineSchedule.IdRecurrencePattern.Value);
                    }

                    // Загружаем выбранные дни недели
                    if (UiState.IsWeekDaysMode && WeekDays.Any() &&
                        !string.IsNullOrEmpty(MedicineSchedule.WeekDays))
                    {
                        // Разбиваем строку с днями недели
                        var selectedDayNames = MedicineSchedule.WeekDays
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(day => day.Trim())
                            .ToList();

                        // Отмечаем выбранные дни
                        foreach (var day in WeekDays)
                        {
                            day.IsSelected = selectedDayNames.Contains(day.Name);
                        }

                        UpdateSelectedDaysText();
                    }

                    // Загружаем выбранные времена
                    if (!string.IsNullOrEmpty(MedicineSchedule.Times))
                    {
                        var times = MedicineSchedule.Times
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => TimeSpan.Parse(t.Trim()))
                            .ToList();

                        SelectedTimes.Clear();
                        foreach (var time in times)
                        {
                            SelectedTimes.Add(time);
                        }

                        // Сортируем
                        SelectedTimes = new ObservableCollection<TimeSpan>(
                            SelectedTimes.OrderBy(t => t));
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Расписание для лекарства не найдено", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось загрузить данные расписания: {ex.Message}", "OK");
            }
        }

        private void ResetForm()
        {
            MedicineSchedule = new MedicineScheduleDto
            {
                IdMedicine = MedicineId, // Устанавливаем MedicineId
                Dosage = 1, // Значение по умолчанию
                ScheduleIsActive = true // По умолчанию активно
            };

            SelectedScheduleType = null;
            SelectedScheduleMode = null;
            SelectedRecurrencePattern = null;

            // Сбрасываем выбранные дни
            ResetSelectedDays();

            // Сбрасываем время
            SelectedTimes.Clear();
            NewTime = TimeSpan.FromHours(8);
            UpdateTimesText();

            UiState.Reset();
        }

        private async Task LoadReferenceData()
        {
            await LoadWeekDaysAsync();
            await LoadRecurrencePatternsAsync();
            await LoadScheduleTypesAsync();
            await LoadScheduleModeAsync();
        }

        private async Task LoadWeekDaysAsync()
        {
            try
            {
                var weekDays = await _referencesDateRepository.GetAllWeekDayAsync();
                WeekDays = new ObservableCollection<WeekDayModel>(weekDays);

                // Подписываемся на изменения выбора дней
                foreach (var day in WeekDays)
                {
                    day.PropertyChanged += OnWeekDaySelectionChanged;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки дней недели: {ex.Message}");
            }
        }

        private async Task LoadRecurrencePatternsAsync()
        {
            try
            {
                var recurrencePattern = await _referencesDateRepository.GetAllRecurrencePatternAsync();
                RecurrencePatterns = new ObservableCollection<RecurrencePatternModel>(recurrencePattern);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки периодичностей повторения: {ex.Message}");
            }
        }

        private async Task LoadScheduleTypesAsync()
        {
            try
            {
                var scheduleTypes = await _referencesDateRepository.GetAllSheduleTypeAsync();
                ScheduleTypes = new ObservableCollection<ScheduleTypeModel>(scheduleTypes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки типов расписания: {ex.Message}");
            }
        }

        private async Task LoadScheduleModeAsync()
        {
            try
            {
                var scheduleModes = await _referencesDateRepository.GetAllScheduleModeAsync();
                ScheduleModes = new ObservableCollection<ScheduleModeModel>(scheduleModes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки режимов расписания: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            // Получаем выбранные дни
            var selectedDays = WeekDays?.Where(d => d.IsSelected).ToList() ?? new List<WeekDayModel>();

            var errors = _validator.GetScheduleValidationErrors(
                UiState.IsRecurringSchedule,
                UiState.IsIntervalMode,
                UiState.IsWeekDaysMode,
                MedicineSchedule.DateStart,
                MedicineSchedule.OneTimeDate,
                SelectedScheduleType,
                SelectedScheduleMode,
                SelectedRecurrencePattern,
                selectedDays,
                SelectedTimes.ToList()); // Добавляем проверку времени

            if (errors.Any())
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                return;
            }

            try
            {
                // Используем сервис для сохранения
                int scheduleId = await _scheduleService.SaveScheduleAsync(
                    MedicineSchedule,
                    selectedDays,
                    SelectedTimes.ToList());

                if (IsEditingExisting)
                {
                    // Возвращаемся назад
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Возвращаемся назад
                    await Shell.Current.GoToAsync("//medicines");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить расписание: {ex.Message}", "OK");
            }
        }
    }
}