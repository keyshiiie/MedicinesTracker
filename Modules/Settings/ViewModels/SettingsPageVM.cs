using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicinesTracker.Models;
using MedicinesTracker.Repository;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MedicinesTracker.Modules.Settings.ViewModels
{
    public partial class SettingsPageVM : ObservableObject
    {
        private readonly IRecipientRepository _recipientRepository;

        [ObservableProperty]
        private ObservableCollection<RecipientModel> _recipients = new();

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
            Recipients = new ObservableCollection<RecipientModel>(recipients);
            Debug.WriteLine($"Загружено получателей лекарств: {Recipients.Count}");
        }

        [RelayCommand]
        private async Task OpenEditPage(RecipientModel recipient)
        {
            try
            {
                var route = "EditRecipientPage";
                var parameters = new Dictionary<string, object>();

                // Если recipient null, создаем новый объект
                if (recipient == null)
                {
                    recipient = new RecipientModel();
                }

                parameters.Add("recipient", recipient);
                await Shell.Current.GoToAsync(route, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переходе на страницу редактирования: {ex.Message}");
            }
        }
    }
}
