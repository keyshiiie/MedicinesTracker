#if ANDROID
using Android.Content;
using MedicinesTracker.Platforms.Android.Services;

namespace MedicinesTracker.Services
{
    public static class ForegroundServiceStarter
    {
        public static void StartForegroundService()
        {
            try
            {
                var context = Android.App.Application.Context;
                var serviceIntent = new Intent(context,
                    Java.Lang.Class.FromType(typeof(ForegroundNotificationService)));

                System.Diagnostics.Debug.WriteLine("Запускаем ForegroundServiceStarter...");

                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    // Используем StartForegroundService для Android 8.0+
                    context.StartForegroundService(serviceIntent);
                    System.Diagnostics.Debug.WriteLine("✅ StartForegroundService вызван (Android O+)");
                }
                else
                {
                    // Для старых версий используем StartService
                    context.StartService(serviceIntent);
                    System.Diagnostics.Debug.WriteLine("✅ StartService вызван (Android < O)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка запуска службы: {ex.Message}");
            }
        }

        public static void StopForegroundService()
        {
            try
            {
                var context = Android.App.Application.Context;
                var serviceIntent = new Intent(context,
                    Java.Lang.Class.FromType(typeof(ForegroundNotificationService)));

                context.StopService(serviceIntent);
                System.Diagnostics.Debug.WriteLine("✅ Служба переднего плана остановлена");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка остановки службы: {ex.Message}");
            }
        }
    }
}
#endif