using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace MedicinesTracker.Platforms.Android
{
    [BroadcastReceiver(
        Name = "com.medicinestracker.NotificationPublisher",
        Enabled = true,
        Exported = false)]
    [IntentFilter(new[]
    {
        Intent.ActionBootCompleted,
        "ACTION_SHOW_NOTIFICATION"
    })]
    public class NotificationPublisher : BroadcastReceiver
    {
        private const string CHANNEL_ID = "medication_reminders";
        private const string CHANNEL_NAME = "Напоминания о приеме лекарств";

        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== NotificationPublisher.OnReceive ===");

                if (context == null || intent == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Context или Intent равен null");
                    return;
                }

                // Если это команда на показ уведомления
                if (intent.Action == "ACTION_SHOW_NOTIFICATION")
                {
                    ShowNotification(context, intent);
                }
                else if (intent.Action == Intent.ActionBootCompleted)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Перезагрузка устройства, служба будет запущена");
                    // Служба запустится при первом открытии приложения
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка в NotificationPublisher: {ex.Message}");
            }
        }

        private void ShowNotification(Context context, Intent intent)
        {
            try
            {
                var medicineName = intent.GetStringExtra("medicine_name") ?? "Лекарство";
                var dosage = intent.GetIntExtra("dosage", 1);
                var recipientName = intent.GetStringExtra("recipient_name") ?? "Пациент";
                var medicineId = intent.GetIntExtra("medicine_id", 0);
                var intakeId = intent.GetIntExtra("intake_id", 0);
                var unitName = intent.GetStringExtra("unit_name") ?? "таблетка(и)"; // Добавляем единицу измерения
                var scheduledTime = intent.GetStringExtra("scheduled_time") ?? DateTime.Now.ToString("HH:mm");

                System.Diagnostics.Debug.WriteLine($"Показываю уведомление: {medicineName} в {scheduledTime}");

                // Создаем канал уведомлений
                CreateNotificationChannel(context);

                // Intent для открытия приложения при нажатии на уведомление
                var openIntent = new Intent(context, typeof(MainActivity));
                openIntent.PutExtra("notification_clicked", true);
                openIntent.PutExtra("medicine_id", medicineId);
                openIntent.PutExtra("intake_id", intakeId);
                openIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    GenerateNotificationId(medicineId, DateTime.Now),
                    openIntent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

                // Иконка для уведомления
                int notificationIcon = global::Android.Resource.Drawable.StatNotifyChat;

                // ✅ ИСПРАВЛЕННЫЙ ТЕКСТ: вместо времени показываем единицу измерения
                var notificationText = $"{medicineName} - {dosage} {unitName} для {recipientName}";

                // Создаем уведомление
                var notificationBuilder = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle("💊 Примите лекарство!")
                    .SetContentText(notificationText)
                    .SetStyle(new NotificationCompat.BigTextStyle()
                        .BigText(notificationText)
                        .SetBigContentTitle("💊 Примите лекарство!")
                        .SetSummaryText($"Запланировано на {scheduledTime}"))
                    .SetSmallIcon(notificationIcon)
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetAutoCancel(true)
                    .SetContentIntent(pendingIntent)
                    .SetDefaults(NotificationCompat.DefaultAll);

                // ✅ ИСПРАВЛЕННЫЙ ПОДЗАГОЛОВОК: показываем время здесь
                notificationBuilder.SetSubText($"Время приёма: {scheduledTime}");

                var notification = notificationBuilder.Build();

                // Показываем уведомление
                var notificationManager = NotificationManagerCompat.From(context);
                var notificationId = GenerateNotificationId(medicineId, DateTime.Now);

                notificationManager.Notify(notificationId, notification);

                System.Diagnostics.Debug.WriteLine($"✅ Уведомление показано: {medicineName} в {scheduledTime}");

                // Отмечаем уведомление как показанное
                MarkNotificationAsShown(context, intakeId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка показа уведомления: {ex.Message}");
            }
        }

        private void MarkNotificationAsShown(Context context, int intakeId)
        {
            try
            {
                var prefs = context.GetSharedPreferences("notifications", FileCreationMode.Private);
                using var editor = prefs.Edit();
                editor.PutBoolean($"shown_{intakeId}", true);
                editor.Commit();
            }
            catch { }
        }

        private void CreateNotificationChannel(Context context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                try
                {
                    var channel = new NotificationChannel(
                        CHANNEL_ID,
                        CHANNEL_NAME,
                        NotificationImportance.High);

                    channel.Description = "Уведомления о времени приема лекарств";

                    // Устанавливаем вибрацию
                    channel.EnableVibration(true);
                    channel.SetVibrationPattern(new long[] { 0, 500, 200, 500 });

                    var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                    notificationManager?.CreateNotificationChannel(channel);

                    System.Diagnostics.Debug.WriteLine("✅ Канал уведомлений создан");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Ошибка создания канала: {ex.Message}");
                }
            }
        }

        private int GenerateNotificationId(int medicineId, DateTime time)
        {
            return Math.Abs($"{medicineId}{time:yyyyMMddHHmm}".GetHashCode());
        }
    }
}