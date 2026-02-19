namespace MedicinesTracker.Modules.Medications.Views.Controls;

public partial class CreationProgressBar : ContentView
{
    public static readonly BindableProperty CurrentStepProperty =
        BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(CreationProgressBar), 1);

    public static readonly BindableProperty TotalStepsProperty =
        BindableProperty.Create(nameof(TotalSteps), typeof(int), typeof(CreationProgressBar), 4);

    public static readonly BindableProperty ProgressWidthProperty =
        BindableProperty.Create(nameof(ProgressWidth), typeof(double), typeof(CreationProgressBar), 0.0);

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set
        {
            SetValue(CurrentStepProperty, value);
            UpdateProgressWidth();
        }
    }

    public int TotalSteps
    {
        get => (int)GetValue(TotalStepsProperty);
        set
        {
            SetValue(TotalStepsProperty, value);
            UpdateProgressWidth();
        }
    }

    public double ProgressWidth
    {
        get => (double)GetValue(ProgressWidthProperty);
        set => SetValue(ProgressWidthProperty, value);
    }

    public CreationProgressBar()
    {
        InitializeComponent();
    }

    private void UpdateProgressWidth()
    {
        if (TotalSteps > 0)
        {
            // ѕолучаем ширину контейнера (минус отступы)
            var containerWidth = (Application.Current?.Windows.FirstOrDefault()?.Width ?? 400) - 40;
            var percentage = (double)CurrentStep / TotalSteps;
            ProgressWidth = containerWidth * percentage;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateProgressWidth();
    }
}