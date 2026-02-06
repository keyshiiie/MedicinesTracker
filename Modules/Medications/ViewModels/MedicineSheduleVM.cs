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
    [QueryProperty(nameof(IsNewMedicine), "isNewMedicine")] // Добавляем флаг
    public partial class MedicineScheduleVM : ObservableObject
    {
        private readonly IScheduleService _scheduleService;
        private readonly IReferencesDataRepository _referencesDateRepository;
        private readonly IValidatorService _validator;
        private readonly IMedicineBuilder _medicineBuilder;

        [ObservableProperty]
        private ScheduleUIState _uiState = new();

        private readonly ScheduleFieldCleaner _fieldCleaner = new();
        private string? _previousScheduleTypeCode;
        private string? _previousScheduleModeCode;

        [ObservableProperty]
        private MedicineScheduleDto _medicineSchedule = new();

        [ObservableProperty]
        private bool _isNewMedicine;


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

        [ObservableProperty]
        private bool _canEditSchedule = true;

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

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private ButtonUiState _saveButtonState = new();

        private bool _isInitialized = false;
        public MedicineScheduleVM(
            IScheduleService scheduleService,
            IReferencesDataRepository referencesDateRepository,
            IValidatorService validatorService,
            IMedicineBuilder medicineBuilder)
        {
            _scheduleService = scheduleService;
            _referencesDateRepository = referencesDateRepository;
            _validator = validatorService;
            _medicineBuilder = medicineBuilder;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MedicineScheduleVM ERROR] {ex.Message}");
            }
        }

        partial void OnScheduleIdChanged(int value)
        {
            _isEditingExisting = value > 0;
            UpdateButtonState();
            CanEditSchedule = !IsEditingExisting;

            if (value >= 0)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        private void UpdateButtonState()
        {
            SaveButtonState.Text = IsEditingExisting ? "Сохранить" : "Продолжить";
            SaveButtonState.IsPrimary = true;
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

        private async Task LoadMedicineDataAsync(int scheduleId)
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
                    // Если расписание не найдено, не нужно сбрасывать форму
                    // Показываем ошибку и возвращаемся назад
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Расписание для лекарства не найдено", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось загрузить данные расписания: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                Debug.WriteLine($"LoadData: MedicineId={MedicineId}, ScheduleId={ScheduleId}, " +
                              $"IsEditingExisting={IsEditingExisting}, IsNewMedicine={IsNewMedicine}");

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
                    // Добавление расписания к существующему лекарству
                    Debug.WriteLine($"Добавление нового расписания для существующего MedicineId={MedicineId}");

                    // Сначала сбрасываем форму
                    ResetForm();

                    // Затем устанавливаем даты по умолчанию
                    SetDefaultDates();

                    // Убедимся, что MedicineId установлен в DTO
                    MedicineSchedule.IdMedicine = MedicineId;
                }
                else if (IsNewMedicine)
                {
                    // Создание нового лекарства через Builder (MedicineId еще не существует)
                    Debug.WriteLine($"Создание нового лекарства через Builder (MedicineId будет создан в конце)");

                    // Сначала сбрасываем форму
                    ResetForm();

                    // Затем устанавливаем даты по умолчанию
                    SetDefaultDates();

                    // MedicineId будет установлен позже из Builder
                    MedicineSchedule.IdMedicine = 0;
                }
                else
                {
                    Debug.WriteLine("Ошибка: не указаны параметры для создания расписания");
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
                var scheduleTypes = await _referencesDateRepository.GetAllScheduleTypeAsync();
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

        // Исправьте метод Save()
        [RelayCommand]
        private async Task Save()
        {
            // Проверяем, не идет ли уже сохранение
            if (IsSaving) return;

            IsSaving = true;

            try
            {
                // Валидация
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
                    SelectedTimes.ToList());

                if (errors.Any())
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                    return;
                }

                // РАЗДЕЛЕНИЕ ЛОГИКИ: РЕДАКТИРОВАНИЕ vs ДОБАВЛЕНИЕ
                if (IsEditingExisting && ScheduleId > 0)
                {
                    // РЕДАКТИРОВАНИЕ - старая логика через IScheduleService
                    Debug.WriteLine("Режим: Редактирование существующего расписания");

                    var scheduleId = await _scheduleService.SaveScheduleAsync(
                        MedicineSchedule,
                        selectedDays,
                        SelectedTimes.ToList());

                    await Shell.Current.DisplayAlertAsync("Успех", "Расписание обновлено", "OK");
                    await Shell.Current.GoToAsync("//medicines");
                }
                else if (MedicineId > 0)
                {
                    // Добавление расписания к существующему лекарству
                    Debug.WriteLine($"Режим: Добавление расписания к существующему лекарству ID={MedicineId}");

                    // Используем ScheduleService
                    MedicineSchedule.IdMedicine = MedicineId;
                    var scheduleId = await _scheduleService.SaveScheduleAsync(
                        MedicineSchedule,
                        selectedDays,
                        SelectedTimes.ToList());

                    await Shell.Current.DisplayAlertAsync("Успех", "Расписание добавлено", "OK");
                    await Shell.Current.GoToAsync("//medicines");
                }
                else if (IsNewMedicine)
                {
                    // ДОБАВЛЕНИЕ нового лекарства через Builder
                    Debug.WriteLine("Режим: Добавление нового лекарства через Builder");

                    // Проверяем, есть ли все данные в Builder
                    var state = _medicineBuilder.GetState();
                    if (state.Medicine == null || state.Stock == null)
                    {
                        await Shell.Current.DisplayAlertAsync("Ошибка",
                            "Сначала заполните основную информацию и запас лекарства", "OK");
                        return;
                    }

                    // Добавляем расписание в Builder (MedicineId будет установлен позже)
                    _medicineBuilder.WithSchedule(MedicineSchedule, selectedDays, SelectedTimes.ToList());

                    Debug.WriteLine($"Schedule добавлен в Builder. Статус готовности: {_medicineBuilder.IsComplete}");

                    // Теперь ВСЕ данные собраны - сохраняем через Builder
                    await SaveAllWithBuilder();
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Ошибка",
                        "Неизвестный режим сохранения", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении расписания: {ex.Message}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось сохранить расписание: {ex.Message}", "OK");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ResetForm()
        {
            MedicineSchedule = new MedicineScheduleDto
            {
                // Для нового лекарства через Builder MedicineId будет 0
                IdMedicine = IsNewMedicine ? 0 : MedicineId,
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

        private async Task SaveAllWithBuilder()
        {
            try
            {
                // Показываем индикатор загрузки
                IsSaving = true;

                // Сохраняем ВСЕ данные через Builder (лекарство + запас + расписание)
                var medicineId = await _medicineBuilder.BuildAsync();

                await Shell.Current.DisplayAlertAsync("Успех",
                    $"Лекарство успешно создано! ID: {medicineId}", "OK");

                // Возвращаемся к списку лекарств
                await Shell.Current.GoToAsync("//medicines");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении через Builder: {ex.Message}");

                var retry = await Shell.Current.DisplayAlertAsync("Ошибка",
                    $"Не удалось сохранить лекарство: {ex.Message}\n\nПопробовать снова?",
                    "Да", "Нет");

                if (retry)
                {
                    await SaveAllWithBuilder();
                }
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void SetDefaultDates()
        {
            var today = DateTime.Now.Date;

            // Для одноразового расписания устанавливаем текущую дату
            if (!IsEditingExisting && (MedicineSchedule.OneTimeDate == null ||
                MedicineSchedule.OneTimeDate == string.Empty))
            {
                MedicineSchedule.OneTimeDate = today.ToString("yyyy-MM-dd");
            }

            // Для повторяющегося расписания
            if (!IsEditingExisting && (MedicineSchedule.DateStart == null ||
                MedicineSchedule.DateStart == string.Empty))
            {
                MedicineSchedule.DateStart = today.ToString("yyyy-MM-dd");
            }

            // Дата окончания - через месяц от сегодняшней
            if (!IsEditingExisting && (MedicineSchedule.DateEnd == null ||
                MedicineSchedule.DateEnd == string.Empty))
            {
                MedicineSchedule.DateEnd = today.AddMonths(1).ToString("yyyy-MM-dd");
            }
        }
    }
}