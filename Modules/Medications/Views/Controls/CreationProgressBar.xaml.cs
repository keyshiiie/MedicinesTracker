using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MedicinesTracker.Modules.Medications.Views.Controls
{
    public partial class CreationProgressBar : ContentView
    {
        public static readonly BindableProperty CurrentStepProperty =
            BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(CreationProgressBar), 1,
                propertyChanged: OnCurrentStepChanged);

        public static readonly BindableProperty TotalStepsProperty =
            BindableProperty.Create(nameof(TotalSteps), typeof(int), typeof(CreationProgressBar), 5,
                propertyChanged: OnTotalStepsChanged);

        public static readonly BindableProperty StepDescriptionProperty =
            BindableProperty.Create(nameof(StepDescription), typeof(string), typeof(CreationProgressBar), string.Empty);

        public static readonly BindableProperty IsEditingProperty =
            BindableProperty.Create(nameof(IsEditing), typeof(bool), typeof(CreationProgressBar), false);

        public int CurrentStep
        {
            get => (int)GetValue(CurrentStepProperty);
            set => SetValue(CurrentStepProperty, value);
        }

        public int TotalSteps
        {
            get => (int)GetValue(TotalStepsProperty);
            set => SetValue(TotalStepsProperty, value);
        }

        public string StepDescription
        {
            get => (string)GetValue(StepDescriptionProperty);
            set => SetValue(StepDescriptionProperty, value);
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public double ProgressWidth => (double)CurrentStep / TotalSteps;

        public CreationProgressBar()
        {
            InitializeComponent();
        }

        private static void OnCurrentStepChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CreationProgressBar)bindable;
            control.OnPropertyChanged(nameof(ProgressWidth));
        }

        private static void OnTotalStepsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CreationProgressBar)bindable;
            control.OnPropertyChanged(nameof(ProgressWidth));
        }
    }
}