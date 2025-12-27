using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MedicinesTracker.Models;
using MedicinesTracker.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MedicinesTracker.Modules.Settings.ViewModels
{
    [QueryProperty(nameof(Recipient), "recipient")]
    public partial class EditRecipientVM : ObservableObject
    {
        private readonly IRecipientRepository _recipientRepository;
        [ObservableProperty]
        private RecipientModel _recipient = new();
        [ObservableProperty]
        private bool _isEditingExisting;
        public EditRecipientVM(IRecipientRepository recipientRepository)
        {
            _recipientRepository = recipientRepository;
        }
        partial void OnRecipientChanged(RecipientModel value)
        {
            // Определяем, редактируем ли мы существующего получателя или добавляем нового
            IsEditingExisting = value?.IdRecipient > 0;
        }
        [RelayCommand]
        private async Task SaveRecipient()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Recipient.Name))
                {
                    Debug.WriteLine("Ошибка: Название получателя не может быть пустым");
                    return;
                }
                int rowsAffected;
                if (Recipient.IdRecipient == 0)
                {
                    Debug.WriteLine("Получатель добавлен");
                    rowsAffected = await _recipientRepository.AddRecipientAsync(Recipient);
                }
                else
                {
                    Debug.WriteLine("Получатель обновлен");
                    rowsAffected = await _recipientRepository.UpdateRecipientAsync(Recipient);
                }
                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        "Получатель успешно сохранен!",
                        "ОК");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Получатель не было обновлен",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка!",
                    $"Не удалось сохранить: {ex.Message}",
                    "ОК");
            }
        }

        [RelayCommand]
        private async Task DeleteRecipient()
        {
            try
            {
                // Сначала запрашиваем подтверждение у пользователя
                bool confirmDelete = await Shell.Current.DisplayAlertAsync(
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить получателя '{Recipient.Name}'? Это удалит все связанные с ним лекарства.",
                    "Да",
                    "Нет"
                );

                // Если пользователь нажал "Нет" — выходим из метода без удаления
                if (!confirmDelete)
                {
                    return;
                }

                // Если пользователь нажал "Да" — выполняем удаление
                var rowsAffected = await _recipientRepository.DeleteRecipientAsync(Recipient.IdRecipient);
                if (rowsAffected > 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Успех!",
                        "Получатель успешно удалён!",
                        "ОК");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Предупреждение!",
                        "Получатель не был удалён",
                        "ОК");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Ошибка!",
                    $"Не удалось удалить: {ex.Message}",
                    "ОК");
            }
        }

    }
}
