using System;

namespace MedicinesTracker.Services
{
    /// <summary>
    /// Интерфейс для планирования уведомлений через AlarmManager (Android)
    /// </summary>
    public interface IAlarmScheduler
    {
        void ScheduleNotification(long triggerTime, object pendingIntent);
        void CancelAllNotifications();
    }
}