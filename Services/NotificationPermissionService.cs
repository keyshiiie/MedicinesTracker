using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace MedicinesTracker.Services
{
    public static class NotificationPermissionService
    {
        public static async Task<bool> CheckAndRequestAsync()
        {
            try
            {
                Console.WriteLine("=== Проверка разрешений на уведомления ===");

                // Только для Android 13+
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
#if ANDROID
                    if (OperatingSystem.IsAndroidVersionAtLeast(33)) // Android 13+
                    {
                        Console.WriteLine("Android 13+ обнаружен, проверяем разрешения...");
                        return await CheckAndRequestAndroid13Async();
                    }
                    else
                    {
                        Console.WriteLine("Android <13 - разрешения не требуются");
                        return true; // Для Android <13 разрешения не нужны
                    }
#endif
                }

                Console.WriteLine("Не Android платформа, разрешения не требуются");
                return true; // Для iOS/Windows всегда true
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки разрешений: {ex.Message}");
                return true; // В случае ошибки возвращаем true
            }
        }

#if ANDROID
        private static async Task<bool> CheckAndRequestAndroid13Async()
        {
            try
            {
                Console.WriteLine("Проверяем статус разрешения POST_NOTIFICATIONS...");

                // Используем статический класс Permissions
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                Console.WriteLine($"Текущий статус: {status}");

                if (status == PermissionStatus.Granted)
                {
                    Console.WriteLine("Разрешение уже предоставлено");
                    return true;
                }

                if (status == PermissionStatus.Denied)
                {
                    Console.WriteLine("Разрешение отклонено, нужно открыть настройки");
                    return false;
                }

                // PermissionStatus.Unknown - еще не запрашивали
                Console.WriteLine("Запрашиваем разрешение...");
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                Console.WriteLine($"Результат запроса: {status}");

                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CheckAndRequestAndroid13Async: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
#endif

        public static void OpenAppSettings()
        {
            try
            {
                Console.WriteLine("Открываем настройки приложения...");
                AppInfo.Current.ShowSettingsUI();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка открытия настроек: {ex.Message}");
            }
        }
    }
}