using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public class DummyAlarmScheduler : IAlarmScheduler
    {
        public void CancelAllNotifications()
        {
            Debug.WriteLine("DummyAlarmScheduler: CancelAllNotifications called");
        }

        public void ScheduleNotification(long triggerTime, object pendingIntent)
        {
            Debug.WriteLine($"DummyAlarmScheduler: ScheduleNotification called for time {triggerTime}");
        }
    }
}