using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Models;
using MedicinesTracker.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicinesTracker.ViewModels.Controls
{
    public partial class RecipientPickerVM : ObservableObject
    {
        private readonly MedicineService _medicineService;
        private bool _isInitialized;

        // Свойства для привязки
        [ObservableProperty]
        private ObservableCollection<RecipientModel> _recipients = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedRecipientId))]
        private RecipientModel? _selectedRecipient;

        // Новое свойство для Id получателя
        public int? SelectedRecipientId => SelectedRecipient?.IdRecipient;

        [ObservableProperty]
        private bool _isLoading;

        // Конструктор
        public RecipientPickerVM(MedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        // Команда загрузки данных (вызывается при тапе на Picker)
        [RelayCommand]
        private async Task LoadData()
        {
            if (_isInitialized) return; // Уже загружено

            _isInitialized = true;
            await LoadRecipientsAsync();
        }

        // Метод загрузки данных
        private async Task LoadRecipientsAsync()
        {
            try
            {
                IsLoading = true;

                var recipients = await _medicineService.GetAllRecipientsAsync();
                Recipients.Clear();
                foreach (var recipient in recipients)
                {
                    Recipients.Add(recipient);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки получателей: {ex.Message}");

                // Можно показать уведомление пользователю
                await Shell.Current.DisplayAlertAsync(
                     "Ошибка",
                     "Не удалось загрузить список получателей. Проверьте подключение к сети.",
                     "ОК");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}