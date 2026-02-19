using Android.App;
using Android.Content;
using Android.OS;
using MedicinesTracker.Services;
using System;

namespace MedicinesTracker.Platforms.Android.Services
{
    public class AlarmScheduler : IAlarmScheduler
    {
        private readonly AlarmManager _alarmManager;
        private readonly Context _context;

        public AlarmScheduler(AlarmManager alarmManager, Context context)
        {
            _alarmManager = alarmManager;
            _context = context;
        }

        public void ScheduleNotification(long triggerTime, object pendingIntentObj)
        {
            try
            {
                var pendingIntent = pendingIntentObj as PendingIntent;
                if (pendingIntent == null) return;

                // Проверяем, можем ли мы использовать точные будильники
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    if (!_alarmManager.CanScheduleExactAlarms())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Нет разрешения на точные будильники");
                        // Используем неточный будильник как запасной вариант
                        _alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pendingIntent);
                        return;
                    }
                }

                // Пробуем использовать точные будильники
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    _alarmManager.SetExactAndAllowWhileIdle(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    _alarmManager.SetExact(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);
                }
                else
                {
                    _alarmManager.Set(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано на {DateTimeOffset.FromUnixTimeMilliseconds(triggerTime).LocalDateTime:HH:mm}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования: {ex.Message}");
                // Пробуем запасной вариант
                try
                {
                    var pendingIntent = pendingIntentObj as PendingIntent;
                    _alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pendingIntent);
                }
                catch { }
            }
        }

        public void CancelAllNotifications()
        {
            try
            {
                var intent = new Intent(_context, typeof(NotificationPublisher));
                intent.SetAction("ACTION_SHOW_NOTIFICATION");

                for (int i = 0; i < 10000; i++)
                {
                    var pendingIntent = PendingIntent.GetBroadcast(
                        _context,
                        i,
                        intent,
                        PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate);

                    if (pendingIntent != null)
                    {
                        _alarmManager.Cancel(pendingIntent);
                        pendingIntent.Cancel();
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ Все уведомления отменены");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка отмены: {ex.Message}");
            }
        }
    }
}