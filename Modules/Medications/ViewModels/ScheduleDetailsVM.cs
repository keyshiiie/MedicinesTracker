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
    [QueryProperty(nameof(ScheduleTypeCode), "scheduleTypeCode")]
    [QueryProperty(nameof(ScheduleModeCode), "scheduleModeCode")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(ScheduleId), "scheduleId")]
    public partial class ScheduleDetailsVM : BaseScheduleStepVM
    {
        private readonly IReferencesDataRepository _referencesRepository;
        private readonly IScheduleService _scheduleService;
        private readonly IMedicineBuilder _medicineBuilder;
        private readonly IValidatorService _validator;

        [ObservableProperty]
        private string _scheduleTypeCode = "RECURRING";

        [ObservableProperty]
        private string? _scheduleModeCode;

        [ObservableProperty]
        private int _medicineId;

        [ObservableProperty]
        private bool _isNewMedicine;

        [ObservableProperty]
        private int _scheduleId;

        [ObservableProperty]
        private MedicineScheduleDto _medicineSchedule = new();

        [ObservableProperty]
        private ObservableCollection<WeekDay> _weekDays = new();

        [ObservableProperty]
        private ObservableCollection<RecurrencePattern> _recurrencePatterns = new();

        [ObservableProperty]
        private RecurrencePattern? _selectedRecurrencePattern;

        [ObservableProperty]
        private ObservableCollection<TimeSpan> _selectedTimes = new();

        [ObservableProperty]
        private TimeSpan _newTime = TimeSpan.FromHours(8);

        [ObservableProperty]
        private string _selectedTimesText = "Не выбрано";

        [ObservableProperty]
        private string _selectedDaysText = "Не выбрано";

        [ObservableProperty]
        private bool _hasSelectedDays;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private bool _isEditingExisting = false;

        public bool IsRecurring => ScheduleTypeCode == "RECURRING";
        public bool IsIntervalMode => IsRecurring && ScheduleModeCode == "INTERVAL";
        public bool IsWeekDaysMode => IsRecurring && ScheduleModeCode == "WEEKDAYS";
        public bool IsOneTime => ScheduleTypeCode == "ONETIME";

        public override string Title => "Расписание";

        public override string Description => IsOneTime
            ? "Настройте одноразовый приём"
            : $"Настройте {GetModeDescription()}";

        private string GetModeDescription()
        {
            return ScheduleModeCode switch
            {
                "INTERVAL" => "интервальное расписание",
                "WEEKDAYS" => "расписание по дням недели",
                _ => "расписание"
            };
        }

        public ScheduleDetailsVM(
            IReferencesDataRepository referencesRepository,
            IScheduleService scheduleService,
            IMedicineBuilder medicineBuilder,
            IValidatorService validatorService)
        {
            _referencesRepository = referencesRepository;
            _scheduleService = scheduleService;
            _medicineBuilder = medicineBuilder;
            _validator = validatorService;
        }

        partial void OnScheduleIdChanged(int value)
        {
            _isEditingExisting = value > 0;
        }

        partial void OnScheduleTypeCodeChanged(string value)
        {
            Debug.WriteLine($"OnScheduleTypeCodeChanged: {value}");
            UpdateUI();

            // При смене типа расписания обновляем даты по умолчанию
            if (!_isEditingExisting)
            {
                SetDefaultDates();
            }
        }

        partial void OnScheduleModeCodeChanged(string? value)
        {
            Debug.WriteLine($"OnScheduleModeCodeChanged: {value}");
            UpdateUI();

            // При смене режима расписания обновляем UI
            OnPropertyChanged(nameof(IsIntervalMode));
            OnPropertyChanged(nameof(IsWeekDaysMode));
        }

        public async Task InitializeAsync()
        {
            Debug.WriteLine($"ScheduleDetailsVM InitializeAsync - Type: {ScheduleTypeCode}, Mode: {ScheduleModeCode}, MedicineId: {MedicineId}, IsNew: {IsNewMedicine}, ScheduleId: {ScheduleId}");

            await LoadDataAsync();

            if (_isEditingExisting && ScheduleId > 0)
            {
                await LoadScheduleDataAsync(ScheduleId);
            }
            else
            {
                // ВАЖНО: Устанавливаем значения по умолчанию ДО загрузки страницы
                SetDefaultDates();

                // Дополнительно проверяем, что значения установились
                Debug.WriteLine($"После SetDefaultDates - Dosage: {MedicineSchedule.Dosage}, IsActive: {MedicineSchedule.ScheduleIsActive}, DateEnd: {MedicineSchedule.DateEnd}");
            }
        }

        private async Task LoadScheduleDataAsync(int scheduleId)
        {
            try
            {
                Debug.WriteLine($"Загружаем расписание ID={scheduleId}");
                var schedule = await _scheduleService.GetScheduleByIdAsync(scheduleId);

                if (schedule != null)
                {
                    MedicineSchedule = schedule;

                    // Загружаем выбранные времена
                    if (!string.IsNullOrEmpty(schedule.Times))
                    {
                        var times = schedule.Times
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => TimeSpan.Parse(t.Trim()))
                            .ToList();

                        SelectedTimes.Clear();
                        foreach (var time in times)
                        {
                            SelectedTimes.Add(time);
                        }

                        SelectedTimes = new ObservableCollection<TimeSpan>(SelectedTimes.OrderBy(t => t));
                        UpdateTimesText();
                    }

                    // Для интервального режима выбираем паттерн
                    if (IsIntervalMode && schedule.IdRecurrencePattern.HasValue && RecurrencePatterns.Any())
                    {
                        SelectedRecurrencePattern = RecurrencePatterns
                            .FirstOrDefault(rp => rp.IdPattern == schedule.IdRecurrencePattern.Value);
                    }

                    // Для режима дней недели отмечаем выбранные дни
                    if (IsWeekDaysMode && !string.IsNullOrEmpty(schedule.WeekDays) && WeekDays.Any())
                    {
                        var selectedDayNames = schedule.WeekDays
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(day => day.Trim())
                            .ToList();

                        foreach (var day in WeekDays)
                        {
                            day.IsSelected = selectedDayNames.Contains(day.Name);
                        }

                        UpdateSelectedDaysText();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки расписания: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось загрузить данные расписания", "OK");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем дни недели
                var days = await _referencesRepository.GetAllWeekDayAsync();
                WeekDays = new ObservableCollection<WeekDay>(days);

                // Подписываемся на изменения выбора дней
                foreach (var day in WeekDays)
                {
                    day.PropertyChanged += OnWeekDaySelectionChanged;
                }

                // Загружаем паттерны повторения только для интервального режима
                if (IsIntervalMode)
                {
                    var patterns = await _referencesRepository.GetAllRecurrencePatternAsync();
                    RecurrencePatterns = new ObservableCollection<RecurrencePattern>(patterns);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            OnPropertyChanged(nameof(IsRecurring));
            OnPropertyChanged(nameof(IsIntervalMode));
            OnPropertyChanged(nameof(IsWeekDaysMode));
            OnPropertyChanged(nameof(IsOneTime));
            OnPropertyChanged(nameof(Description));
        }

        private void SetDefaultDates()
        {
            var today = DateTime.Now.Date;

            if (IsOneTime)
            {
                MedicineSchedule.OneTimeDate = today.ToString("yyyy-MM-dd");
            }
            else
            {
                MedicineSchedule.DateStart = today.ToString("yyyy-MM-dd");
                // Дата завершения на месяц позже
                MedicineSchedule.DateEnd = today.AddMonths(1).ToString("yyyy-MM-dd");
            }

            // Доза по умолчанию = 1 (только если не установлена)
            if (MedicineSchedule.Dosage <= 0)
            {
                MedicineSchedule.Dosage = 1;
            }

            // Уведомления включены по умолчанию (только если не установлены)
            MedicineSchedule.ScheduleIsActive = true;

            // Устанавливаем время по умолчанию, если нет выбранных времен
            if (SelectedTimes.Count == 0)
            {
                SelectedTimes.Add(TimeSpan.FromHours(8));
                UpdateTimesText();
            }

            Debug.WriteLine($"SetDefaultDates - Dosage: {MedicineSchedule.Dosage}, IsActive: {MedicineSchedule.ScheduleIsActive}, DateEnd: {MedicineSchedule.DateEnd}");

            // ВАЖНО: Уведомляем UI об изменениях
            OnPropertyChanged(nameof(MedicineSchedule));
        }

        partial void OnNewTimeChanged(TimeSpan value)
        {
            AddTime(value);
        }

        private void AddTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero && SelectedTimes.Count == 0)
                return;

            if (!SelectedTimes.Contains(time))
            {
                SelectedTimes.Add(time);
                SelectedTimes = new ObservableCollection<TimeSpan>(
                    SelectedTimes.OrderBy(t => t));
                UpdateTimesText();
            }
        }

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

        private void OnWeekDaySelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WeekDay.IsSelected))
            {
                UpdateSelectedDaysText();
            }
        }

        private void UpdateSelectedDaysText()
        {
            var selectedDays = WeekDays.Where(d => d.IsSelected).ToList();
            HasSelectedDays = selectedDays.Any();

            if (HasSelectedDays)
            {
                SelectedDaysText = string.Join(", ", selectedDays.Select(d => d.Name));
            }
            else
            {
                SelectedDaysText = "Не выбрано";
            }
        }

        public override async Task ContinueAsync()
        {
            if (IsSaving) return;

            IsSaving = true;

            try
            {
                Debug.WriteLine($"ScheduleDetailsVM ContinueAsync - Type: {ScheduleTypeCode}, Mode: {ScheduleModeCode}, IsNewMedicine: {IsNewMedicine}, MedicineId: {MedicineId}");

                // ВАЖНО: НЕ меняем значения пользователя!
                // Просто используем те значения, которые пользователь установил в UI

                var selectedDays = WeekDays?.Where(d => d.IsSelected).ToList() ?? new List<WeekDay>();

                var tempScheduleType = new ScheduleType
                {
                    IdType = GetScheduleTypeId(),
                    Name = IsRecurring ? "Повторяющееся" : "Одноразовое",
                    Code = ScheduleTypeCode
                };

                ScheduleMode? tempScheduleMode = null;
                if (IsRecurring && !string.IsNullOrEmpty(ScheduleModeCode))
                {
                    tempScheduleMode = new ScheduleMode
                    {
                        IdMode = GetScheduleModeId() ?? 0,
                        Name = ScheduleModeCode == "INTERVAL" ? "Интервал" : "Дни недели",
                        Code = ScheduleModeCode
                    };
                }

                var errors = _validator.GetScheduleValidationErrors(
                IsRecurring,
                IsIntervalMode,
                IsWeekDaysMode,
                MedicineSchedule.DateStart,
                MedicineSchedule.DateEnd,  
                MedicineSchedule.OneTimeDate,
                MedicineSchedule.Dosage,
                tempScheduleType,
                tempScheduleMode,
                SelectedRecurrencePattern,
                selectedDays,
                SelectedTimes.ToList());

                if (errors.Any())
                {
                    Debug.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                var selectedDaysList = WeekDays.Where(d => d.IsSelected).ToList();

                if (IsNewMedicine)
                {
                    Debug.WriteLine($"Создание нового лекарства через Builder");

                    MedicineSchedule.IdScheduleType = GetScheduleTypeId();
                    MedicineSchedule.IdScheduleMode = GetScheduleModeId();

                    if (IsIntervalMode && SelectedRecurrencePattern != null)
                    {
                        MedicineSchedule.IdRecurrencePattern = SelectedRecurrencePattern.IdPattern;
                    }

                    _medicineBuilder.WithSchedule(MedicineSchedule, selectedDaysList, SelectedTimes.ToList());

                    var state = _medicineBuilder.GetState();
                    Debug.WriteLine($"Builder состояние - Medicine: {state.Medicine != null}, Stock: {state.Stock != null}, Schedule: {state.Schedule != null}, IsComplete: {state.IsComplete}");

                    await SaveAllWithBuilder();
                }
                else if (MedicineId > 0)
                {
                    Debug.WriteLine($"Добавление расписания к существующему лекарству ID={MedicineId}");

                    MedicineSchedule.IdMedicine = MedicineId;
                    MedicineSchedule.IdScheduleType = GetScheduleTypeId();
                    MedicineSchedule.IdScheduleMode = GetScheduleModeId();

                    if (IsIntervalMode && SelectedRecurrencePattern != null)
                    {
                        MedicineSchedule.IdRecurrencePattern = SelectedRecurrencePattern.IdPattern;
                    }

                    // Сохраняем ТО, ЧТО ПОЛЬЗОВАТЕЛЬ ВВЕЛ, без изменений
                    await _scheduleService.SaveScheduleAsync(
                        MedicineSchedule,
                        selectedDaysList,
                        SelectedTimes.ToList());

                    await Shell.Current.DisplayAlertAsync("Успех", "Расписание сохранено!", "OK");
                    await Shell.Current.GoToAsync("//medicines");
                }
                else
                {
                    Debug.WriteLine($"Ошибка: MedicineId не установлен для IsNewMedicine=false");
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Не удалось определить лекарство для сохранения расписания", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving schedule: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить расписание: {ex.Message}", "OK");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task SaveAllWithBuilder()
        {
            try
            {
                var state = _medicineBuilder.GetState();

                if (!_medicineBuilder.IsComplete)
                {
                    var errorMsg = "Не все данные заполнены для создания лекарства.\n";
                    if (state.Medicine == null) errorMsg += "- Базовая информация\n";
                    if (state.Stock == null) errorMsg += "- Запас\n";
                    if (state.Schedule == null) errorMsg += "- Расписание\n";

                    Debug.WriteLine($"Builder не готов: {errorMsg}");
                    await Shell.Current.DisplayAlertAsync("Ошибка", errorMsg, "OK");
                    return;
                }

                Debug.WriteLine("Builder готов к сохранению...");

                var medicineId = await _medicineBuilder.BuildAsync();

                await Shell.Current.DisplayAlertAsync("Успех",
                    $"Лекарство успешно создано!", "OK");

                await Shell.Current.GoToAsync("//medicines");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении через Builder: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить лекарство: {ex.Message}", "OK");
            }
        }

        private int GetScheduleTypeId()
        {
            return ScheduleTypeCode switch
            {
                "ONETIME" => 2,
                "RECURRING" => 1,
                _ => 1
            };
        }

        private int? GetScheduleModeId()
        {
            return ScheduleModeCode switch
            {
                "INTERVAL" => 1,
                "WEEKDAYS" => 2,
                _ => null
            };
        }

        public override async Task BackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}