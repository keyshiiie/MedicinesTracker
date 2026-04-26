using MedicinesTracker.Repository;
using MedicinesTracker.Dto;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface INotificationPlannerService
    {
        Task PlanForTodayAsync();
        void CancelAll();
        Task PlanForDateAsync(DateTime date);
        Task CancelNotificationForIntakeAsync(int intakeId, int medicineId, string scheduledTime);
        Task CancelAllNotificationsForMedicineAsync(int medicineId);
    }

    public class NotificationPlannerService : INotificationPlannerService
    {
        private readonly IMedicineRepository _medicineRepository;
        private readonly IScheduleEvaluator _scheduleEvaluator;
        private readonly IAlarmScheduler _alarmScheduler;

        public NotificationPlannerService(
            IMedicineRepository medicineRepository,
            IScheduleEvaluator scheduleEvaluator,
            IAlarmScheduler alarmScheduler)
        {
            _medicineRepository = medicineRepository;
            _scheduleEvaluator = scheduleEvaluator;
            _alarmScheduler = alarmScheduler;
        }

        public async Task PlanForTodayAsync()
        {
            _alarmScheduler.CancelAllNotifications();
            await PlanForDateAsync(DateTime.Today);
        }

        public async Task PlanForDateAsync(DateTime date)
        {
            var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();

            foreach (var medicine in medicines.Where(m => _scheduleEvaluator.ShouldTakeOnDate(m, date)))
            {
                var times = _scheduleEvaluator.GetMedicationTimes(medicine);

                foreach (var time in times)
                {
                    if (DateTime.TryParse($"{date:yyyy-MM-dd} {time}", out var notificationTime))
                    {
                        if (notificationTime > DateTime.Now)
                        {
                            ScheduleNotification(medicine, notificationTime);
                        }
                    }
                }
            }
        }

        private void ScheduleNotification(MedicineWithScheduleDto medicine, DateTime time)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;

                var intent = new Android.Content.Intent(context, typeof(MedicinesTracker.Platforms.Android.NotificationPublisher));
                intent.SetAction("ACTION_SHOW_NOTIFICATION");
                intent.PutExtra("medicine_name", medicine.MedicineName);
                intent.PutExtra("dosage", medicine.Dosage);
                intent.PutExtra("recipient_name", medicine.RecipientName);
                intent.PutExtra("unit_name", medicine.UnitName);
                intent.PutExtra("scheduled_time", time.ToString("HH:mm"));
                intent.PutExtra("medicine_id", medicine.IdMedicine);

                var requestCode = (medicine.IdMedicine * 100) + time.Hour * 10 + time.Minute / 10;

                var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                    context,
                    requestCode,
                    intent,
                    Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.UpdateCurrent);

                var triggerTime = (long)(time.ToUniversalTime() -
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

                _alarmScheduler.ScheduleNotification(triggerTime, pendingIntent);

                Debug.WriteLine($"✅ Запланировано уведомление: {medicine.MedicineName} в {time:HH:mm}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка планирования: {ex.Message}");
            }
#endif
        }

        public async Task CancelNotificationForIntakeAsync(int intakeId, int medicineId, string scheduledTime)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;

                if (alarmManager == null) return;

                var intent = new Android.Content.Intent(context, typeof(MedicinesTracker.Platforms.Android.NotificationPublisher));
                intent.SetAction("ACTION_SHOW_NOTIFICATION");

                var time = TimeSpan.Parse(scheduledTime);
                var requestCode = (medicineId * 100) + time.Hours * 10 + time.Minutes / 10;

                var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                    context,
                    requestCode,
                    intent,
                    Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.NoCreate);

                if (pendingIntent != null)
                {
                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                    Debug.WriteLine($"✅ Уведомление для intake {intakeId} отменено");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка отмены уведомления: {ex.Message}");
            }
#endif
            await Task.CompletedTask;
        }

        public void CancelAll()
        {
            _alarmScheduler.CancelAllNotifications();
        }

        public async Task CancelAllNotificationsForMedicineAsync(int medicineId)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;

                if (alarmManager == null) return;

                // Получаем расписания лекарства
                var schedules = await _medicineRepository.GetSchedulesByMedicineIdAsync(medicineId);

                foreach (var schedule in schedules)
                {
                    // Для каждого времени приёма отменяем уведомление
                    var times = schedule.Times.Split(',');
                    foreach (var timeStr in times)
                    {
                        if (TimeSpan.TryParse(timeStr.Trim(), out var time))
                        {
                            var requestCode = (medicineId * 100) + time.Hours * 10 + time.Minutes / 10;

                            var intent = new Android.Content.Intent(context, typeof(MedicinesTracker.Platforms.Android.NotificationPublisher));
                            intent.SetAction("ACTION_SHOW_NOTIFICATION");

                            var pendingIntent = Android.App.PendingIntent.GetBroadcast(
                                context,
                                requestCode,
                                intent,
                                Android.App.PendingIntentFlags.Immutable | Android.App.PendingIntentFlags.NoCreate);

                            if (pendingIntent != null)
                            {
                                alarmManager.Cancel(pendingIntent);
                                pendingIntent.Cancel();
                                Debug.WriteLine($"✅ Уведомление для лекарства {medicineId} в {time} отменено");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Ошибка отмены уведомлений для лекарства {medicineId}: {ex.Message}");
            }
#endif
            await Task.CompletedTask;
        }
    }
}