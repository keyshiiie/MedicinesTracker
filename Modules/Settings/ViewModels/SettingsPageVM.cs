using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Entities;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Settings.ViewModels
{
    public partial class SettingsPageVM : ObservableObject
    {
        private readonly IRecipientRepository _recipientRepository;

        [ObservableProperty]
        private ObservableCollection<Recipient> _recipients = new();

        public SettingsPageVM(IRecipientRepository recipientRepository) 
        { 
            _recipientRepository = recipientRepository;
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

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                await LoadRecipientsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                throw;
            }
        }
        private async Task LoadRecipientsAsync()
        {
            var recipients = await _recipientRepository.GetAllRecipientsAsync();
            Recipients = new ObservableCollection<Recipient>(recipients);
        }

        [RelayCommand]
        private async Task OpenEditPage(Recipient recipient)
        {
            try
            {
                Debug.WriteLine($"OpenEditPage called with recipient: {recipient?.Name ?? "null"}");

                var parameters = new Dictionary<string, object>();

                if (recipient == null)
                {
                    recipient = new Recipient();
                    Debug.WriteLine("Creating new recipient");
                }

                parameters.Add("recipient", recipient);

                Debug.WriteLine($"Navigating to EditRecipientPage");
                await Shell.Current.GoToAsync("EditRecipientPage", parameters);
                Debug.WriteLine("Navigation completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось открыть страницу: {ex.Message}", "OK");
            }
        }
    }
}
