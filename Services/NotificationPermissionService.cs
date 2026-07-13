using Microsoft.Maui.Devices;
#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace MedicinesTracker.Services
{
    public static class NotificationPermissionService
    {
        public static async Task<bool> CheckAndRequestAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Проверка разрешений на уведомления ===");

                // Только для Android 13+
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
#if ANDROID
                    if (OperatingSystem.IsAndroidVersionAtLeast(33)) // Android 13+
                    {
                        System.Diagnostics.Debug.WriteLine("Android 13+ обнаружен, проверяем разрешения...");
                        return await CheckAndRequestAndroid13Async();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Android <13 - разрешения не требуются");
                        return true; // Для Android <13 разрешения не нужны
                    }
#endif
                }

                System.Diagnostics.Debug.WriteLine("Не Android платформа, разрешения не требуются");
                return true; // Для iOS/Windows всегда true
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки разрешений: {ex.Message}");
                return false; // В случае ошибки возвращаем false
            }
        }

#if ANDROID
        private static async Task<bool> CheckAndRequestAndroid13Async()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Проверяем статус разрешения POST_NOTIFICATIONS...");

                // Проверяем текущий статус
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                System.Diagnostics.Debug.WriteLine($"Текущий статус: {status}");

                if (status == PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("Разрешение уже предоставлено");
                    return true;
                }

                if (status == PermissionStatus.Denied)
                {
                    // Если разрешение отклонено, пытаемся запросить снова
                    System.Diagnostics.Debug.WriteLine("Разрешение ранее отклонено, запрашиваем снова...");

                    // Показываем объяснение пользователю
                    bool shouldRequest = await ShowPermissionExplanation();
                    if (!shouldRequest)
                    {
                        return false;
                    }

                    status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                    System.Diagnostics.Debug.WriteLine($"Результат запроса: {status}");

                    return status == PermissionStatus.Granted;
                }

                // PermissionStatus.Unknown - еще не запрашивали
                System.Diagnostics.Debug.WriteLine("Запрашиваем разрешение впервые...");

                // Показываем объяснение перед запросом
                bool shouldRequestFirstTime = await ShowPermissionExplanation();
                if (!shouldRequestFirstTime)
                {
                    return false;
                }

                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                System.Diagnostics.Debug.WriteLine($"Результат запроса: {status}");

                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в CheckAndRequestAndroid13Async: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private static async Task<bool> ShowPermissionExplanation()
        {
            try
            {
                // Используем MainThread для вывода диалога
                bool result = false;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    result = await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlertAsync(
                        "Разрешение на уведомления",
                        "Приложению нужно разрешение на отправку уведомлений, " +
                        "чтобы напоминать вам о времени приема лекарств.\n\n" +
                        "Без этого разрешения напоминания не будут работать.",
                        "Разрешить",
                        "Не сейчас");
                });

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка показа объяснения: {ex.Message}");
                return true; // Если не получилось показать, все равно запрашиваем
            }
        }
#endif

        public static async Task<bool> RequestExactAlarmPermissionAsync()
        {
            try
            {
#if ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    var context = Android.App.Application.Context;
                    var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;

                    if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Запрашиваем разрешение на точные будильники");

                        // Открываем настройки для разрешения
                        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                        intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                        intent.SetFlags(ActivityFlags.NewTask);
                        context.StartActivity(intent);

                        return false;
                    }
                }
#endif
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка запроса разрешения: {ex.Message}");
                return false;
            }
        }

        public static void OpenAppSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Открываем настройки приложения...");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AppInfo.Current.ShowSettingsUI();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия настроек: {ex.Message}");
            }
        }
    }
}