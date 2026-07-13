using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Threading.Tasks;
using Android.Views;
using Android.Media;
// Убираем using Android.Graphics;

namespace MedicinesTracker.Platforms.Android
{
    [BroadcastReceiver(
        Name = "com.medicinestracker.NotificationPublisher",
        Enabled = true,
        Exported = false)]
    [IntentFilter(new[] { Intent.ActionBootCompleted, "ACTION_SHOW_NOTIFICATION" })]
    public class NotificationPublisher : BroadcastReceiver
    {
        private PowerManager.WakeLock _wakeLock;
        private const string CHANNEL_ID = "medication_reminders";
        private const string CHANNEL_NAME = "Напоминания о лекарствах";
        private const string CHANNEL_DESC = "Уведомления о времени приема лекарств";

        public override async void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                if (context == null || intent == null) return;

                if (intent.Action == Intent.ActionBootCompleted)
                {
                    await RescheduleAllNotifications(context);
                    return;
                }

                if (intent.Action == "ACTION_SHOW_NOTIFICATION")
                {
                    // Получаем wake lock для включения экрана
                    AcquireWakeLock(context);

                    // Небольшая задержка для гарантии включения экрана
                    await Task.Delay(100);

                    ShowNotification(context, intent);

                    // Освобождаем wake lock через некоторое время
                    ReleaseWakeLock(3000);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }

        private void AcquireWakeLock(Context context)
        {
            try
            {
                var powerManager = (PowerManager)context.GetSystemService(Context.PowerService);
                if (powerManager != null)
                {
                    _wakeLock = powerManager.NewWakeLock(
                        WakeLockFlags.ScreenBright | WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup,
                        "MedicinesTracker:NotificationWakeLock");

                    _wakeLock.Acquire(5000); // Удерживаем максимум 5 секунд

                    System.Diagnostics.Debug.WriteLine("🔆 WakeLock acquired - screen should turn on");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WakeLock error: {ex.Message}");
            }
        }

        private void ReleaseWakeLock(int delayMs)
        {
            Task.Delay(delayMs).ContinueWith(_ =>
            {
                try
                {
                    _wakeLock?.Release();
                    System.Diagnostics.Debug.WriteLine("🔆 WakeLock released");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ WakeLock release error: {ex.Message}");
                }
            });
        }

        private async Task RescheduleAllNotifications(Context context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Перезагрузка устройства - перепланируем уведомления");

                var startIntent = new Intent(context, typeof(MainActivity));
                startIntent.SetFlags(ActivityFlags.NewTask);
                startIntent.PutExtra("reschedule_notifications", true);
                context.StartActivity(startIntent);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка перепланировки: {ex.Message}");
            }
        }

        private void ShowNotification(Context context, Intent intent)
        {
            var medicineName = intent.GetStringExtra("medicine_name") ?? "Лекарство";
            var dosage = intent.GetIntExtra("dosage", 1);
            var recipientName = intent.GetStringExtra("recipient_name") ?? "Пациент";
            var unitName = intent.GetStringExtra("unit_name") ?? "таб.";
            var scheduledTime = intent.GetStringExtra("scheduled_time") ?? "";

            // Явно указываем System.Math чтобы избежать конфликта с Java.Lang.Math
            var notificationId = System.Math.Abs((medicineName + scheduledTime).GetHashCode());

            CreateChannel(context);

            var openIntent = new Intent(context, typeof(MainActivity));
            openIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                0,
                openIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            // Используем системную иконку
            int iconId = global::Android.Resource.Drawable.StatNotifySync;

            // Создаем уведомление с расширенными настройками
            var notificationBuilder = new NotificationCompat.Builder(context, CHANNEL_ID)
                .SetContentTitle($"💊 Пора принять {medicineName}")
                .SetContentText($"{dosage} {unitName} для {recipientName}")
                .SetSmallIcon(iconId)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryAlarm)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent)
                .SetDefaults(NotificationCompat.DefaultAll); // Включает звук, вибрацию и светодиод по умолчанию

            // Для Android 8+ добавляем полноэкранный интент для гарантии пробуждения
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var fullScreenIntent = new Intent(context, typeof(MainActivity));
                fullScreenIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                fullScreenIntent.PutExtra("from_notification", true);

                var fullScreenPendingIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    fullScreenIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

                notificationBuilder.SetFullScreenIntent(fullScreenPendingIntent, true);
            }

            // Добавляем действия для уведомления
            var acceptIntent = new Intent(context, typeof(NotificationPublisher));
            acceptIntent.SetAction("ACTION_ACCEPT_MEDICINE");
            acceptIntent.PutExtra("medicine_name", medicineName);
            acceptIntent.PutExtra("scheduled_time", scheduledTime);

            var acceptPendingIntent = PendingIntent.GetBroadcast(
                context,
                notificationId + 1,
                acceptIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            // Используем системные иконки для действий
            notificationBuilder.AddAction(
                new NotificationCompat.Action(
                    global::Android.Resource.Drawable.IcDialogDialer,
                    "Принял(а)",
                    acceptPendingIntent));

            var notification = notificationBuilder.Build();

            var notificationManager = NotificationManagerCompat.From(context);

            notificationManager.Notify(notificationId, notification);
            System.Diagnostics.Debug.WriteLine($"✅ Уведомление показано: {medicineName} в {scheduledTime}");
        }

        private void CreateChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High);

                // Устанавливаем описание
                channel.Description = CHANNEL_DESC;

                channel.EnableVibration(true);

                // Устанавливаем паттерн вибрации
                long[] vibrationPattern = new long[] { 0, 500, 200, 500 };
                channel.SetVibrationPattern(vibrationPattern);

                // Устанавливаем звук для канала
                var audioAttributes = new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Notification)
                    .SetContentType(AudioContentType.Sonification)
                    .Build();

                var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
                channel.SetSound(defaultSoundUri, audioAttributes);

                var manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                manager?.CreateNotificationChannel(channel);

                System.Diagnostics.Debug.WriteLine($"✅ Канал уведомлений создан: {CHANNEL_ID}");
            }
        }
    }
}