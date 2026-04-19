// Services/StepManager.cs
using System;

namespace MedicinesTracker.Services
{
    public class StepManager
    {
        private int _currentStep = 1;
        private readonly Dictionary<int, StepInfo> _steps = new()
        {
            { 1, new StepInfo { Title = "Базовая информация", Description = "Шаг 1 из 5: Укажите название и основные характеристики лекарства" } },
            { 2, new StepInfo { Title = "Запас лекарства", Description = "Шаг 2 из 5: Укажите текущий запас и порог напоминания" } },
            { 3, new StepInfo { Title = "Тип расписания", Description = "Шаг 3 из 5: Выберите тип расписания приёма" } },
            { 4, new StepInfo { Title = "Способ задания", Description = "Шаг 4 из 5: Выберите способ задания расписания" } },
            { 5, new StepInfo { Title = "Детали расписания", Description = "Шаг 5 из 5: Настройте детали расписания приёма" } }
        };

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value && value >= 1 && value <= 5)
                {
                    _currentStep = value;
                    OnStepChanged?.Invoke(_currentStep);
                    OnStepInfoChanged?.Invoke(GetCurrentStepInfo());
                }
            }
        }

        public int TotalSteps => 5;

        public StepInfo GetCurrentStepInfo() => _steps[_currentStep];

        public event Action<int>? OnStepChanged;
        public event Action<StepInfo>? OnStepInfoChanged;

        public void NextStep()
        {
            if (CurrentStep < TotalSteps)
                CurrentStep++;
        }

        public void PreviousStep()
        {
            if (CurrentStep > 1)
                CurrentStep--;
        }

        public void Reset()
        {
            CurrentStep = 1;
        }
    }

    public class StepInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}