using Android.App;
using Android.Content;
using Android.OS;

namespace MedicinesTracker.Platforms.Android.Services
{
    public interface IAlarmScheduler
    {
        void ScheduleExactAlarm(long triggerTime, PendingIntent pendingIntent);
        void CancelAlarm(PendingIntent pendingIntent);
        bool CanScheduleExactAlarms();
    }

    public class AlarmScheduler : IAlarmScheduler
    {
        private readonly Context _context;
        private readonly AlarmManager _alarmManager;

        public AlarmScheduler(Context context, AlarmManager alarmManager)
        {
            _context = context;
            _alarmManager = alarmManager;
        }

        public bool CanScheduleExactAlarms()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                return _alarmManager.CanScheduleExactAlarms();
            }
            return true;
        }

        public void ScheduleExactAlarm(long triggerTime, PendingIntent pendingIntent)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Планируем точное уведомление на время: {triggerTime}");

                // Используем самый точный метод для Android 6.0+
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    _alarmManager.SetExactAndAllowWhileIdle(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);

                    System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExactAndAllowWhileIdle");
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    _alarmManager.SetExact(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);

                    System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExact");
                }
                else
                {
                    _alarmManager.Set(
                        AlarmType.RtcWakeup,
                        triggerTime,
                        pendingIntent);

                    System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с set");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования: {ex.Message}");
                throw;
            }
        }

        public void CancelAlarm(PendingIntent pendingIntent)
        {
            try
            {
                _alarmManager.Cancel(pendingIntent);
                System.Diagnostics.Debug.WriteLine("✅ Уведомление отменено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка отмены: {ex.Message}");
            }
        }
    }
}