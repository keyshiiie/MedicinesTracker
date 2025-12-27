using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Models.Dto;
using MedicinesTracker.Modules.Medications.Models;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Medications.ViewModels
{
    [QueryProperty(nameof(MedicineId), "idMedicine")]
    [QueryProperty(nameof(ScheduleId), "idSchedule")]
    [QueryProperty(nameof(UnitName), "unitName")]
    public partial class MedicineScheduleVM : ObservableObject
    {
        private readonly IMedicineScheduleRepository _medicineScheduleRepository;
        private readonly IReferencesDataRepository _referencesDateRepository;

        [ObservableProperty]
        private ScheduleUIState _uiState = new();

        private readonly ScheduleFieldCleaner _fieldCleaner = new();
        private string? _previousScheduleTypeCode;
        private string? _previousScheduleModeCode;

        private readonly ScheduleValidator _validator = new();

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
        private WeekDayModel? _selectedWeekDays;

        [ObservableProperty]
        private ObservableCollection<RecurrencePatternModel> _recurrencePatterns = new();

        [ObservableProperty]
        private RecurrencePatternModel? _selectedRecurrencePattern;

        [ObservableProperty]
        private bool _isEditingExisting;

        private bool _isInitialized = false;

        [ObservableProperty]
        private bool _canEditSchedule = true;

        public MedicineScheduleVM(IMedicineScheduleRepository medicineSheduleRepository,
            IReferencesDataRepository referencesDateRepository)
        {
            _medicineScheduleRepository = medicineSheduleRepository;
            _referencesDateRepository = referencesDateRepository;
        }

        // Обработка изменения MedicineId
        partial void OnScheduleIdChanged(int value)
        {
            IsEditingExisting = value > 0;
            // При добавлении - можно редактировать, при редактировании - нет
            CanEditSchedule = !IsEditingExisting;

            if (value >= 0 && !_isInitialized)
            {
                _isInitialized = true;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await LoadData();
                });
            }
        }

        partial void OnSelectedScheduleModeChanged(ScheduleModeModel? value)
        {
            if (value != null)
            {
                // Очищаем неактивные поля через FieldCleaner
                _fieldCleaner.CleanForScheduleMode(
                value.Code,
                _previousScheduleModeCode,
                ref _selectedWeekDays,
                ref _selectedRecurrencePattern);

                // Обновляем UI состояние
                UiState.UpdateForScheduleMode(value.Code);

                // Сохраняем текущий режим для следующего сравнения
                _previousScheduleModeCode = value.Code;
            }
            else
            {
                UiState.UpdateForScheduleMode(null);
                _previousScheduleModeCode = null;
            }
        }

        partial void OnSelectedScheduleTypeChanged(ScheduleTypeModel? value)
        {
            if (value != null)
            {
                // Очищаем поля через FieldCleaner
                _fieldCleaner.CleanForScheduleType(
                    value.Code,
                    _previousScheduleTypeCode,
                    MedicineSchedule,
                    ref _selectedScheduleMode,
                    ref _selectedRecurrencePattern,
                    ref _selectedWeekDays);

                // Обновляем UI состояние
                UiState.UpdateForScheduleType(value.Code);

                // Сохраняем текущий тип для следующего сравнения
                _previousScheduleTypeCode = value.Code;
            }
        }

        [RelayCommand]
        public async Task LoadData()
        {
            try
            {
                Debug.WriteLine($"LoadData вызван. Режим: {(IsEditingExisting ? "Редактирование" : "Добавление")}");

                // Загружаем справочные данные только если они еще не загружены
                if (!ScheduleTypes.Any() || !WeekDays.Any() || !RecurrencePatterns.Any())
                {
                    await LoadReferenceData();
                }

                // Если это редактирование, загружаем данные лекарства
                if (IsEditingExisting && MedicineId > 0)
                {
                    await LoadMedicineDataAsync(MedicineId);
                }
                else
                {
                    // Если это добавление, сбрасываем модель
                    ResetForm();
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
                // Загружаем данные лекарства по ID
                var medicine = await _medicineScheduleRepository.GetMedicineScheduleById(medicineId);

                if (medicine != null)
                {
                    MedicineSchedule = medicine;

                    // Сначала устанавливаем тип расписания
                    if (ScheduleTypes.Any())
                    {
                        SelectedScheduleType = ScheduleTypes
                            .FirstOrDefault(st => st.Code == MedicineSchedule.ScheduleTypeCode);

                        // После установки типа, IsRecurringSchedule установится автоматически
                    }

                    // Затем устанавливаем режим расписания (только для RECURRING)
                    if (UiState.IsRecurringSchedule && ScheduleModes.Any() &&
                        !string.IsNullOrEmpty(MedicineSchedule.ScheduleModeCode))
                    {
                        SelectedScheduleMode = ScheduleModes
                            .FirstOrDefault(sm => sm.Code == MedicineSchedule.ScheduleModeCode);
                        // IsIntervalMode и IsWeekDaysMode установятся автоматически
                    }

                    // Устанавливаем периодичность для INTERVAL режима
                    if (UiState.IsIntervalMode && RecurrencePatterns.Any() &&
                        MedicineSchedule.IdRecurrencePattern.HasValue)
                    {
                        SelectedRecurrencePattern = RecurrencePatterns
                            .FirstOrDefault(rp => rp.IdPattern == MedicineSchedule.IdRecurrencePattern.Value);
                    }

                    // Устанавливаем день недели для WEEKDAYS режима
                    if (UiState.IsWeekDaysMode && WeekDays.Any() &&
                        !string.IsNullOrEmpty(MedicineSchedule.WeekDays))
                    {
                        // WeekDays может содержать несколько дней через запятую
                        // Выбираем первый день для отображения в пикере
                        var firstDay = MedicineSchedule.WeekDays?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .FirstOrDefault()?
                            .Trim();

                        if (!string.IsNullOrEmpty(firstDay))
                        {
                            SelectedWeekDays = WeekDays
                                .FirstOrDefault(wd => wd.Name == firstDay);
                        }
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
            // Сбрасываем модель для нового лекарства
            MedicineSchedule = new MedicineScheduleDto();

            // Сбрасываем выбранные значения в пикерах
            SelectedScheduleType = null;
            SelectedScheduleMode = null;
            SelectedRecurrencePattern = null;
            SelectedWeekDays = null;

            // Сбрасываем флаги
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

        // Команды для кнопок
        [RelayCommand]
        private async Task Save()
        {
            // валидация
            var errors = _validator.GetValidationErrors(
                UiState.IsRecurringSchedule,
                UiState.IsIntervalMode,
                UiState.IsWeekDaysMode,
                MedicineSchedule.DateStart,
                MedicineSchedule.OneTimeDate,
                SelectedScheduleType,
                SelectedScheduleMode,
                SelectedRecurrencePattern,
                SelectedWeekDays);
            if (errors.Any())
            {
                await Shell.Current.DisplayAlertAsync("Ошибка", string.Join("\n", errors), "OK");
                return;
            }

        }
    }
}