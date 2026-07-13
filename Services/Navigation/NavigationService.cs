using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MedicinesTracker.Services.Navigation
{
    public interface INavigationService
    {
        Task GoToAsync(string route, Dictionary<string, object>? parameters = null);
        Task GoBackAsync();
        Task ShowAlertAsync(string title, string message);
        Task<bool> ShowConfirmationAsync(string title, string message);
    }

    public class NavigationService : INavigationService
    {
        private DateTime _lastNavigationTime = DateTime.MinValue;
        private const int NAVIGATION_DEBOUNCE_MS = 300;

        public async Task GoToAsync(string route, Dictionary<string, object>? parameters = null)
        {
            try
            {
                // Защита от двойного нажатия
                if ((DateTime.Now - _lastNavigationTime).TotalMilliseconds < NAVIGATION_DEBOUNCE_MS)
                {
                    Debug.WriteLine($"Navigation debounced: {route}");
                    return;
                }

                _lastNavigationTime = DateTime.Now;

                if (parameters?.Any() == true)
                    await Shell.Current.GoToAsync(route, true, parameters); // true = анимация
                else
                    await Shell.Current.GoToAsync(route, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Navigation Error] {route}: {ex.Message}");
                await ShowAlertAsync("Ошибка", $"Не удалось открыть страницу: {ex.Message}");
            }
        }

        public async Task GoBackAsync()
        {
            await GoToAsync("..");
        }

        public async Task GoToModalAsync(string route, Dictionary<string, object>? parameters = null)
        {
            try
            {
                if (parameters?.Any() == true)
                    await Shell.Current.GoToAsync(route, true, parameters);
                else
                    await Shell.Current.GoToAsync(route, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Modal Navigation Error] {route}: {ex.Message}");
                await ShowAlertAsync("Ошибка", $"Не удалось открыть окно: {ex.Message}");
            }
        }

        public async Task ShowAlertAsync(string title, string message)
        {
            await Shell.Current.DisplayAlertAsync(title, message, "OK");
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return await Shell.Current.DisplayAlertAsync(title, message, "Да", "Нет");
        }
    }
}