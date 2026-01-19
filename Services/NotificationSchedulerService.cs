// Services/NotificationSchedulerService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedicinesTracker.Repository;
using MedicinesTracker.Models.Dto;
#if ANDROID
using Android.App;
using Android.Content;
#endif

namespace MedicinesTracker.Services
{
    public interface INotificationSchedulerService
    {
        Task InitializeAsync();
        Task ScheduleNotificationsForTodayAsync();
        Task ScheduleAllNotificationsAsync();
        void CancelAllNotifications();
    }

    public class NotificationSchedulerService : INotificationSchedulerService
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IMedicineRepository _medicineRepository;
        private bool _isInitialized = false;

        public NotificationSchedulerService(
            IIntakeRepository intakeRepository,
            IMedicineRepository medicineRepository)
        {
            _intakeRepository = intakeRepository;
            _medicineRepository = medicineRepository;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            System.Diagnostics.Debug.WriteLine("=== NotificationSchedulerService.InitializeAsync ===");
            _isInitialized = true;
            await ScheduleAllNotificationsAsync();
        }

        public async Task ScheduleAllNotificationsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ScheduleAllNotificationsAsync начато ===");

                // Получаем все активные лекарства с расписанием
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();

                System.Diagnostics.Debug.WriteLine($"Найдено лекарств с расписанием: {medicines?.Count() ?? 0}");

                // Преобразуем в List для использования
                var medicineList = medicines.ToList();

                // Планируем на ближайшие 7 дней
                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.Today.AddDays(i);
                    System.Diagnostics.Debug.WriteLine($"Планируем на дату: {date:yyyy-MM-dd}");
                    await ScheduleNotificationsForDateAsync(date, medicineList);
                }

                System.Diagnostics.Debug.WriteLine("=== ScheduleAllNotificationsAsync завершено ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка планирования уведомлений: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        public async Task ScheduleNotificationsForTodayAsync()
        {
            try
            {
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();
                await ScheduleNotificationsForDateAsync(DateTime.Today, medicineList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка планирования уведомлений на сегодня: {ex.Message}");
            }
        }

        private async Task ScheduleNotificationsForDateAsync(DateTime date, List<MedicineWithScheduleDto> medicines)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            System.Diagnostics.Debug.WriteLine($"=== Планирование для даты: {dateStr} (Сегодня: {DateTime.Today:yyyy-MM-dd}) ===");

            foreach (var medicine in medicines)
            {
                if (!medicine.ScheduleIsActive)
                    continue;

                var times = ParseTimes(medicine.Times);
                System.Diagnostics.Debug.WriteLine($"Лекарство: {medicine.MedicineName}, Времена: {string.Join(", ", times)}");

                // Проверяем, нужно ли принимать лекарство сегодня
                var shouldTake = ShouldTakeMedicine(medicine, date);

                System.Diagnostics.Debug.WriteLine($"Принимать {medicine.MedicineName} {dateStr}: {shouldTake}");

                if (!shouldTake)
                    continue;

                foreach (var time in times)
                {
                    var notificationTime = DateTime.Parse($"{dateStr} {time}");
                    System.Diagnostics.Debug.WriteLine($"  Время: {time}, Полное время: {notificationTime}");

                    // УБЕРИТЕ ЭТУ ПРОВЕРКУ ДЛЯ СЕГОДНЯШНИХ УВЕДОМЛЕНИЙ:
                    // if (date.Date == DateTime.Today && notificationTime < DateTime.Now)
                    //     continue;

                    // Для отладки: планируем уведомления даже если время прошло
                    await ScheduleNotificationAsync(medicine, notificationTime);
                }
            }
        }



        private bool ShouldTakeMedicine(MedicineWithScheduleDto medicine, DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            if (!medicine.ScheduleIsActive)
            {
                System.Diagnostics.Debug.WriteLine($"  Расписание не активно");
                return false;
            }

            if (medicine.ScheduleTypeCode == "ONETIME")
            {
                var result = medicine.OneTimeDate == dateStr;
                System.Diagnostics.Debug.WriteLine($"  Одноразовое: {result} (OneTimeDate: {medicine.OneTimeDate}, date: {dateStr})");
                return result;
            }
            else if (medicine.ScheduleTypeCode == "RECURRING")
            {
                System.Diagnostics.Debug.WriteLine($"  Проверка повторяющегося расписания:");

                // Проверяем период действия
                if (!string.IsNullOrEmpty(medicine.DateStart))
                {
                    var startDate = DateTime.Parse(medicine.DateStart);
                    if (date < startDate)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Дата {dateStr} раньше начала {medicine.DateStart}");
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(medicine.DateEnd))
                {
                    var endDate = DateTime.Parse(medicine.DateEnd);
                    if (date > endDate)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Дата {dateStr} позже окончания {medicine.DateEnd}");
                        return false;
                    }
                }

                if (medicine.ScheduleModeCode == "INTERVAL" && medicine.DaysInterval.HasValue)
                {
                    DateTime referenceDate;
                    if (!string.IsNullOrEmpty(medicine.OneTimeDate))
                    {
                        referenceDate = DateTime.Parse(medicine.OneTimeDate);
                    }
                    else if (!string.IsNullOrEmpty(medicine.DateStart))
                    {
                        referenceDate = DateTime.Parse(medicine.DateStart);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    Нет reference date");
                        return false;
                    }

                    var daysDiff = (date - referenceDate).Days;
                    var result = daysDiff % medicine.DaysInterval.Value == 0;

                    System.Diagnostics.Debug.WriteLine($"    Интервал {medicine.DaysInterval} дней: {result} (daysDiff: {daysDiff})");
                    return result;
                }
                else if (medicine.ScheduleModeCode == "WEEKDAYS" && !string.IsNullOrEmpty(medicine.WeekDayIds))
                {
                    var dayNumber = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var dayIds = medicine.WeekDayIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var num) ? num : 0)
                        .Where(id => id > 0);

                    var result = dayIds.Contains(dayNumber);
                    System.Diagnostics.Debug.WriteLine($"    Дни недели: {result} (dayNumber: {dayNumber}, WeekDayIds: {medicine.WeekDayIds})");
                    return result;
                }

                // Если режим не указан или не распознан, принимается каждый день
                System.Diagnostics.Debug.WriteLine($"    Режим не указан, принимается каждый день: true");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"    Неизвестный тип расписания: {medicine.ScheduleTypeCode}");
            return false;
        }

        private string[] ParseTimes(string? timesString)
        {
            if (string.IsNullOrEmpty(timesString))
                return new[] { "08:00" };

            return timesString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        private async Task ScheduleNotificationAsync(MedicineWithScheduleDto medicine, DateTime notificationTime)
        {
#if ANDROID
    try
    {
        var context = Android.App.Application.Context;
        var alarmManager = context.GetSystemService(Context.AlarmService) as Android.App.AlarmManager;

        if (alarmManager == null)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AlarmManager не найден!");
            return;
        }

        // Используем полное имя класса с пространством имен
        var intent = new Intent(context, 
            Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));
        
        intent.SetAction($"NOTIFICATION_{medicine.IdMedicine}_{notificationTime.Ticks}");
        intent.PutExtra("medicine_id", medicine.IdMedicine);
        intent.PutExtra("medicine_name", medicine.MedicineName);
        intent.PutExtra("dosage", medicine.Dosage);
        intent.PutExtra("recipient_name", medicine.RecipientName);

        // Уникальный ID для каждого уведомления
        var requestCode = GenerateNotificationId(medicine.IdMedicine, notificationTime);
        
        System.Diagnostics.Debug.WriteLine($"Создаем PendingIntent для: {medicine.MedicineName}");
        System.Diagnostics.Debug.WriteLine($"Время: {notificationTime}");
        System.Diagnostics.Debug.WriteLine($"RequestCode: {requestCode}");

        // ВАЖНО: Используем FLAG_IMMUTABLE для Android 12+
        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            requestCode,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        // Конвертируем время в миллисекунды
        var triggerTime = GetDateTimeInMillis(notificationTime);
        var nowMillis = GetDateTimeInMillis(DateTime.Now);
        
        System.Diagnostics.Debug.WriteLine($"Триггерное время (мс): {triggerTime}");
        System.Diagnostics.Debug.WriteLine($"Текущее время (мс): {nowMillis}");
        System.Diagnostics.Debug.WriteLine($"Разница (мс): {triggerTime - nowMillis}");
        System.Diagnostics.Debug.WriteLine($"Разница (сек): {(triggerTime - nowMillis) / 1000}");

        // Если время уже прошло, планируем на 30 секунд вперед
        if (triggerTime <= nowMillis)
        {
            triggerTime = nowMillis + 30000; // 30 секунд для теста
            System.Diagnostics.Debug.WriteLine($"Время прошло, планируем на 30 секунд вперед: {triggerTime}");
        }

        // Проверяем разрешение SCHEDULE_EXACT_ALARM для Android 12+
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            var canSchedule = alarmManager.CanScheduleExactAlarms();
            System.Diagnostics.Debug.WriteLine($"CanScheduleExactAlarms: {canSchedule}");
            
            if (!canSchedule)
            {
                // Показываем диалог для запроса разрешения
                System.Diagnostics.Debug.WriteLine("❌ Нет разрешения на точные уведомления!");
                // Можно показать диалог пользователю
                return;
            }
        }

        // Используем setExactAndAllowWhileIdle для точного времени
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(
                Android.App.AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);
            
            System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExactAndAllowWhileIdle");
        }
        else
        {
            alarmManager.SetExact(
                Android.App.AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);
            
            System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExact");
        }

        System.Diagnostics.Debug.WriteLine($"✅ Запланировано уведомление: {medicine.MedicineName} на {notificationTime}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
    }
#endif
            await Task.CompletedTask;
        }

        private int GenerateNotificationId(int medicineId, DateTime time)
        {
            return $"{medicineId}{time:HHmm}".GetHashCode();
        }

#if ANDROID
        private long GetDateTimeInMillis(DateTime dateTime)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(utcTime - epoch).TotalMilliseconds;
        }
#endif

        public void CancelAllNotifications()
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Context.AlarmService) as Android.App.AlarmManager;

                if (alarmManager == null)
                    return;

                // Создаем Intent с тем же типом для отмены
                var intent = new Intent(context, Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));
                var pendingIntent = PendingIntent.GetBroadcast(
                    context,
                    0,
                    intent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate);

                if (pendingIntent != null)
                {
                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отмены уведомлений: {ex.Message}");
            }
#endif
        }

        public async Task DebugLogMedicinesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DebugLogMedicinesAsync ===");

                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();

                System.Diagnostics.Debug.WriteLine($"Всего лекарств: {medicineList.Count}");

                foreach (var medicine in medicineList)
                {
                    System.Diagnostics.Debug.WriteLine($"Лекарство: {medicine.MedicineName}");
                    System.Diagnostics.Debug.WriteLine($"  ID: {medicine.IdMedicine}");
                    System.Diagnostics.Debug.WriteLine($"  Расписание активно: {medicine.ScheduleIsActive}");
                    System.Diagnostics.Debug.WriteLine($"  Тип расписания: {medicine.ScheduleTypeCode}");
                    System.Diagnostics.Debug.WriteLine($"  Режим расписания: {medicine.ScheduleModeCode}");
                    System.Diagnostics.Debug.WriteLine($"  Времена: {medicine.Times}");
                    System.Diagnostics.Debug.WriteLine($"  Начало: {medicine.DateStart}");
                    System.Diagnostics.Debug.WriteLine($"  Окончание: {medicine.DateEnd}");
                    System.Diagnostics.Debug.WriteLine($"  Интервал дней: {medicine.DaysInterval}");
                    System.Diagnostics.Debug.WriteLine($"  Дни недели: {medicine.WeekDayIds}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка DebugLogMedicinesAsync: {ex.Message}");
            }
        }
    }
}