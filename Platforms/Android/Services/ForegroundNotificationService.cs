using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace MedicinesTracker.Platforms.Android.Services
{
    [Service(Name = "com.medicinestracker.ForegroundNotificationService",
             Enabled = true,
             Exported = false,
             ForegroundServiceType = ForegroundService.TypeDataSync)]
    public class ForegroundNotificationService : Service
    {
        private const int SERVICE_ID = 1001;
        private const string CHANNEL_ID = "medication_foreground_service";
        private const string CHANNEL_NAME = "Служба напоминаний";

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        [System.Obsolete]
        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            System.Diagnostics.Debug.WriteLine("=== ForegroundNotificationService.OnStartCommand ===");

            try
            {
                // ВАЖНО: Для Android O+ нужно вызвать StartForeground() в течение 5 секунд
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    CreateNotificationChannel();
                    var notification = CreateForegroundNotification();
                    StartForeground(SERVICE_ID, notification);
                    System.Diagnostics.Debug.WriteLine("✅ Служба запущена в foreground режиме (Android O+)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ Служба уведомлений инициализирована (Android < O)");
                }

                // Планируем уведомления
                ScheduleAllNotifications();

                return StartCommandResult.Sticky;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка в OnStartCommand: {ex.Message}");
                // Все равно возвращаем Sticky, чтобы служба перезапускалась
                return StartCommandResult.Sticky;
            }
        }

        private void ScheduleAllNotifications()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Планируем уведомления через AlarmManager...");

                // Используем AlarmManager для планирования
                var alarmManager = GetSystemService(AlarmService) as AlarmManager;
                if (alarmManager != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ AlarmManager доступен для планирования");

                    // Здесь можно запланировать периодические проверки
                    SchedulePeriodicCheck(alarmManager);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка планирования: {ex.Message}");
            }
        }

        private void SchedulePeriodicCheck(AlarmManager alarmManager)
        {
            try
            {
                // Планируем проверку каждые 6 часов
                var triggerTime = Java.Lang.JavaSystem.CurrentTimeMillis() + (6 * 60 * 60 * 1000);

                var intent = new Intent(this, typeof(NotificationPublisher));
                intent.SetAction("PERIODIC_CHECK");

                var pendingIntent = PendingIntent.GetBroadcast(
                    this,
                    0,
                    intent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    alarmManager.SetExactAndAllowWhileIdle(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);
                }
                else
                {
                    alarmManager.SetExact(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Периодическая проверка запланирована через 6 часов");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования периодической проверки: {ex.Message}");
            }
        }

        public override void OnDestroy()
        {
            System.Diagnostics.Debug.WriteLine("=== ForegroundNotificationService.OnDestroy ===");
            StopForeground(true);
            base.OnDestroy();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                try
                {
                    var channel = new NotificationChannel(
                        CHANNEL_ID,
                        CHANNEL_NAME,
                        NotificationImportance.Low)
                    {
                        Description = "Служба напоминаний о приеме лекарств",
                        LockscreenVisibility = NotificationVisibility.Public
                    };

                    var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                    notificationManager?.CreateNotificationChannel(channel);

                    System.Diagnostics.Debug.WriteLine("✅ Канал уведомлений для службы создан");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка создания канала: {ex.Message}");
                }
            }
        }

        private Notification CreateForegroundNotification()
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            // Исправленная строка: используем правильную иконку
            // Для .NET MAUI нужно использовать Resource.Drawable или другую системную иконку
            int serviceIcon = global::Android.Resource.Drawable.StatNotifySync; // Используем системную иконку

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("Напоминания о лекарствах")
                .SetContentText("Служба напоминаний активна")
                .SetSmallIcon(serviceIcon)
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetCategory(NotificationCompat.CategoryService)
                .SetOnlyAlertOnce(true);

            return builder.Build();
        }
    }
}