using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Dto;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using MedicinesTracker.Services;
using MedicinesTracker.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MedicinesTracker.Constants;
using static MedicinesTracker.Constants.ScheduleTypes;
using static MedicinesTracker.Constants.ScheduleModes;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(ScheduleTypeCode), "scheduleTypeCode")]
    [QueryProperty(nameof(ScheduleModeCode), "scheduleModeCode")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")]
    [QueryProperty(nameof(ScheduleId), "scheduleId")]
    public partial class ScheduleDetailsVM : CreationStepBaseVM
    {
        private readonly IReferencesDataRepository _referencesRepository;
        private readonly IScheduleService _scheduleService;
        private readonly IMedicineBuilder _medicineBuilder;
        private readonly IValidatorService _validator;
        private readonly IMedicationCreationNavigationService _medicationNavigation;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private string _scheduleTypeCode = Recurring;

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
        private string _dosageError = string.Empty;

        [ObservableProperty]
        private bool _hasDosageError;

        [ObservableProperty]
        private string _datesError = string.Empty;

        [ObservableProperty]
        private bool _hasDatesError;

        public bool IsRecurring => ScheduleTypeCode == Recurring;
        public bool IsIntervalMode => IsRecurring && ScheduleModeCode == Interval;
        public bool IsWeekDaysMode => IsRecurring && ScheduleModeCode == Weekly;
        public bool IsOneTime => ScheduleTypeCode == OneTime;

        private bool _isInitialized = false;
        private bool _isDataLoading = false;

        public ScheduleDetailsVM(
            IReferencesDataRepository referencesRepository,
            IScheduleService scheduleService,
            IMedicineBuilder medicineBuilder,
            IValidatorService validatorService,
            StepManager stepManager,
            IMedicationCreationNavigationService medicationNavigation,
            INavigationService navigation) : base(stepManager, navigation)
        {
            _referencesRepository = referencesRepository;
            _scheduleService = scheduleService;
            _medicineBuilder = medicineBuilder;
            _validator = validatorService;
            _medicationNavigation = medicationNavigation;
            _navigation = navigation;
        }

        partial void OnScheduleIdChanged(int value)
        {
            IsEditingExisting = value > 0;
        }

        partial void OnScheduleTypeCodeChanged(string value)
        {
            Debug.WriteLine($"OnScheduleTypeCodeChanged: {value}");
            UpdateUI();

            if (!IsEditingExisting)
            {
                SetDefaultDates();
            }
        }

        partial void OnScheduleModeCodeChanged(string? value)
        {
            Debug.WriteLine($"OnScheduleModeCodeChanged: {value}");
            UpdateUI();
            OnPropertyChanged(nameof(IsIntervalMode));
            OnPropertyChanged(nameof(IsWeekDaysMode));
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            Debug.WriteLine($"ScheduleDetailsVM InitializeAsync...");

            IsEditingExisting = ScheduleId > 0;

            if (IsNewMedicine && _stepManager.CurrentStep != 5)
            {
                _stepManager.CurrentStep = 5;
            }

            await LoadDataAsync();

            if (IsEditingExisting && ScheduleId > 0)
            {
                await LoadScheduleDataAsync(ScheduleId);
            }
            else
            {
                SetDefaultDates();
            }

            _isInitialized = true;
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

                    if (IsIntervalMode && schedule.IdRecurrencePattern.HasValue && RecurrencePatterns.Any())
                    {
                        SelectedRecurrencePattern = RecurrencePatterns
                            .FirstOrDefault(rp => rp.IdPattern == schedule.IdRecurrencePattern.Value);
                    }

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
                await _navigation.ShowAlertAsync("Ошибка", "Не удалось загрузить данные расписания");
            }
        }

        private async Task LoadDataAsync()
        {
            if (_isDataLoading) return;

            _isDataLoading = true;

            try
            {
                var days = await _referencesRepository.GetAllWeekDayAsync();
                WeekDays = new ObservableCollection<WeekDay>(days);

                foreach (var day in WeekDays)
                {
                    day.PropertyChanged += OnWeekDaySelectionChanged;
                }

                if (IsIntervalMode)
                {
                    var patterns = await _referencesRepository.GetAllRecurrencePatternAsync();
                    RecurrencePatterns = new ObservableCollection<RecurrencePattern>(patterns);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось загрузить данные: {ex.Message}");
            }
            finally
            {
                _isDataLoading = false;
            }
        }

        private void UpdateUI()
        {
            OnPropertyChanged(nameof(IsRecurring));
            OnPropertyChanged(nameof(IsIntervalMode));
            OnPropertyChanged(nameof(IsWeekDaysMode));
            OnPropertyChanged(nameof(IsOneTime));
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
                MedicineSchedule.DateEnd = today.AddMonths(1).ToString("yyyy-MM-dd");
            }

            if (MedicineSchedule.Dosage <= 0)
            {
                MedicineSchedule.Dosage = ValidationConstants.MinDosage;
            }

            MedicineSchedule.ScheduleIsActive = true;

            if (SelectedTimes.Count == 0)
            {
                SelectedTimes.Add(TimeSpan.FromHours(8));
                UpdateTimesText();
            }

            OnPropertyChanged(nameof(MedicineSchedule));
        }

        [RelayCommand]
        private void ValidateDosage(string text)
        {
            var (isValid, errorMessage, dosage) = _validator.ValidateDosageField(text);

            DosageError = errorMessage;
            HasDosageError = !isValid;

            if (isValid)
            {
                MedicineSchedule.Dosage = dosage;
            }
        }

        private bool ValidateAllFields(out string errorMessage)
        {
            // Валидация дозировки
            if (HasDosageError || MedicineSchedule.Dosage < ValidationConstants.MinDosage)
            {
                errorMessage = "Укажите корректную дозировку";
                return false;
            }

            // Валидация времени
            var timesResult = _validator.ValidateTimesField(SelectedTimes);
            if (!timesResult.IsValid)
            {
                errorMessage = timesResult.ErrorMessage;
                return false;
            }

            // Валидация выбранных дней недели
            var weekDaysResult = _validator.ValidateWeekDaysField(WeekDays.ToList(), IsWeekDaysMode);
            if (!weekDaysResult.IsValid)
            {
                errorMessage = weekDaysResult.ErrorMessage;
                return false;
            }

            // Валидация частоты приёма
            var patternResult = _validator.ValidateRecurrencePatternField(SelectedRecurrencePattern, IsIntervalMode);
            if (!patternResult.IsValid)
            {
                errorMessage = patternResult.ErrorMessage;
                return false;
            }

            // Валидация дат
            var datesResult = _validator.ValidateDatesField(IsOneTime, MedicineSchedule.OneTimeDate, MedicineSchedule.DateStart, MedicineSchedule.DateEnd);
            if (!datesResult.IsValid)
            {
                errorMessage = datesResult.ErrorMessage;
                return false;
            }

            errorMessage = string.Empty;
            return true;
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
                var validation = _validator.ValidateTimesField(SelectedTimes);
                if (SelectedTimes.Count >= ValidationConstants.MaxTimesCount)
                {
                    _navigation.ShowAlertAsync("Внимание", validation.ErrorMessage);
                    return;
                }

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
            SelectedTimesText = SelectedTimes.Count > 0
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
            HasSelectedDays = selectedDays.Count > 0;

            SelectedDaysText = HasSelectedDays
                ? string.Join(", ", selectedDays.Select(d => d.Name))
                : "Не выбрано";
        }

        public override async Task ContinueAsync()
        {
            if (IsSaving) return;

            IsSaving = true;

            try
            {
                Debug.WriteLine($"ScheduleDetailsVM ContinueAsync...");

                if (!ValidateAllFields(out var error))
                {
                    await _navigation.ShowAlertAsync("Ошибка", error);
                    return;
                }

                var selectedDaysList = WeekDays.Where(d => d.IsSelected).ToList();

                if (IsNewMedicine)
                {
                    await CreateNewMedicineWithSchedule(selectedDaysList);
                }
                else if (MedicineId > 0)
                {
                    await AddScheduleToExistingMedicine(selectedDaysList);
                }
                else
                {
                    await _navigation.ShowAlertAsync("Ошибка", "Не удалось определить лекарство для сохранения расписания");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving schedule: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось сохранить расписание: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task CreateNewMedicineWithSchedule(List<WeekDay> selectedDaysList)
        {
            MedicineSchedule.IdScheduleType = ScheduleTypes.GetId(ScheduleTypeCode);
            MedicineSchedule.IdScheduleMode = ScheduleModes.GetId(ScheduleModeCode);

            if (IsIntervalMode && SelectedRecurrencePattern != null)
            {
                MedicineSchedule.IdRecurrencePattern = SelectedRecurrencePattern.IdPattern;
            }

            _medicineBuilder.WithSchedule(MedicineSchedule, selectedDaysList, SelectedTimes.ToList());
            await SaveAllWithBuilder();
        }

        private async Task AddScheduleToExistingMedicine(List<WeekDay> selectedDaysList)
        {
            MedicineSchedule.IdMedicine = MedicineId;
            MedicineSchedule.IdScheduleType = ScheduleTypes.GetId(ScheduleTypeCode);
            MedicineSchedule.IdScheduleMode = ScheduleModes.GetId(ScheduleModeCode);

            if (IsIntervalMode && SelectedRecurrencePattern != null)
            {
                MedicineSchedule.IdRecurrencePattern = SelectedRecurrencePattern.IdPattern;
            }

            await _scheduleService.SaveScheduleAsync(MedicineSchedule, selectedDaysList, SelectedTimes.ToList());

            await _navigation.ShowAlertAsync("Успех", "Расписание сохранено!");
            _stepManager.Reset();
            await _medicationNavigation.BackToMedicineListAsync();
        }

        private async Task SaveAllWithBuilder()
        {
            try
            {
                if (!_medicineBuilder.IsComplete)
                {
                    var state = _medicineBuilder.GetState();
                    var errorMsg = "Не все данные заполнены для создания лекарства.\n";
                    if (state.Medicine == null) errorMsg += "- Базовая информация\n";
                    if (state.Stock == null) errorMsg += "- Запас\n";
                    if (state.Schedule == null) errorMsg += "- Расписание\n";

                    await _navigation.ShowAlertAsync("Ошибка", errorMsg);
                    return;
                }

                await _medicineBuilder.BuildAsync();

                await _navigation.ShowAlertAsync("Успех", "Лекарство успешно создано!");
                _stepManager.Reset();
                await _medicationNavigation.BackToMedicineListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении: {ex.Message}");
                await _navigation.ShowAlertAsync("Ошибка", $"Не удалось сохранить лекарство: {ex.Message}");
            }
        }

        public override async Task BackAsync()
        {
            if (IsNewMedicine)
            {
                _stepManager.PreviousStep();
            }
            await _navigation.GoBackAsync();
        }
    }
}