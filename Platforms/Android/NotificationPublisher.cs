#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System;

namespace MedicinesTracker.Platforms.Android
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class NotificationPublisher : BroadcastReceiver
    {
        private const string CHANNEL_ID = "medication_reminders";
        private const string CHANNEL_NAME = "Напоминания о приеме лекарств";

        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== NotificationPublisher.OnReceive ===");
                System.Diagnostics.Debug.WriteLine($"Действие: {intent.Action}");
                System.Diagnostics.Debug.WriteLine($"Время: {DateTime.Now}");

                var medicineName = intent.GetStringExtra("medicine_name") ?? "Лекарство";
                var dosage = intent.GetIntExtra("dosage", 1);
                var recipientName = intent.GetStringExtra("recipient_name") ?? "Пациент";

                System.Diagnostics.Debug.WriteLine($"Лекарство: {medicineName}");
                System.Diagnostics.Debug.WriteLine($"Дозировка: {dosage}");
                System.Diagnostics.Debug.WriteLine($"Получатель: {recipientName}");

                // Создаем канал уведомлений
                CreateNotificationChannel(context);

                // Получаем иконку
                int iconId = GetNotificationIconId(context);
                System.Diagnostics.Debug.WriteLine($"Иконка ID: {iconId}");

                // Создаем уведомление
                var notificationBuilder = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle("💊 Время приема лекарства")
                    .SetContentText($"{recipientName}: {medicineName} - {dosage} шт.")
                    .SetPriority((int)NotificationPriority.High)
                    .SetAutoCancel(true)
                    .SetSmallIcon(iconId);

                var notification = notificationBuilder.Build();

                // Показываем уведомление
                var notificationManager = NotificationManagerCompat.From(context);
                var notificationId = Math.Abs(DateTime.Now.Ticks.GetHashCode());

                System.Diagnostics.Debug.WriteLine($"Показываю уведомление ID: {notificationId}");

                notificationManager.Notify(notificationId, notification);

                System.Diagnostics.Debug.WriteLine($"✅ Уведомление отправлено: {medicineName} в {DateTime.Now}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка в NotificationPublisher: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private int GetNotificationIconId(Context context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Получаем ID иконки...");

                // Способ 1: Используем вашу SVG иконку
                var iconId = context.Resources.GetIdentifier("notification_icon", "drawable", context.PackageName);
                if (iconId != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Найдена иконка: notification_icon (ID: {iconId})");
                    return iconId;
                }

                // Способ 2: Используем иконку приложения
                iconId = context.Resources.GetIdentifier("appicon", "mipmap", context.PackageName);
                if (iconId != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Найдена иконка: appicon (ID: {iconId})");
                    return iconId;
                }

                // Способ 3: Дефолтная иконка MAUI
                iconId = context.ApplicationInfo.Icon;
                if (iconId != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Используется иконка приложения (ID: {iconId})");
                    return iconId;
                }

                // Способ 4: Системная иконка
                System.Diagnostics.Debug.WriteLine("Используется системная иконка StatNotifyMore");
                return global::Android.Resource.Drawable.StatNotifyMore;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения иконки: {ex.Message}");
                return global::Android.Resource.Drawable.StatNotifyMore;
            }
        }

        private void CreateNotificationChannel(Context context)
        {
            System.Diagnostics.Debug.WriteLine("Создаем канал уведомлений...");

            // Для Android 8.0+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    CHANNEL_NAME,
                    NotificationImportance.High)
                {
                    Description = "Уведомления о времени приема лекарств"
                };

                // Настройка вибрации и светодиода
                channel.EnableVibration(true);
                channel.SetVibrationPattern(new long[] { 0, 500, 200, 500 });
                channel.EnableLights(true);
                channel.LightColor = global::Android.Graphics.Color.Green;

                var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                if (notificationManager != null)
                {
                    notificationManager.CreateNotificationChannel(channel);
                    System.Diagnostics.Debug.WriteLine("✅ Канал уведомлений создан");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ NotificationManager не найден");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Android версия < 8.0, канал не нужен");
            }
        }
    }
}
#endif