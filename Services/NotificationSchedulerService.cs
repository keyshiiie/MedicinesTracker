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
        Task DebugLogMedicinesAsync();
        Task CancelNotificationForIntakeAsync(int intakeId);
        // В интерфейс INotificationSchedulerService добавьте:
        Task CancelNotificationsForMedicineAsync(int medicineId);
        Task ScheduleNotificationsForMedicineAsync(int medicineId);
    }

    public class NotificationSchedulerService : INotificationSchedulerService
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IMedicineRepository _medicineRepository;
        private readonly IScheduleTimeRepository _scheduleTimeRepository;
        private bool _isInitialized = false;

        public NotificationSchedulerService(
            IIntakeRepository intakeRepository,
            IMedicineRepository medicineRepository,
            IScheduleTimeRepository scheduleTimeRepository)
        {
            _intakeRepository = intakeRepository;
            _medicineRepository = medicineRepository;
            _scheduleTimeRepository = scheduleTimeRepository;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            System.Diagnostics.Debug.WriteLine("=== NotificationSchedulerService.InitializeAsync ===");
            _isInitialized = true;
            await ScheduleNotificationsForTodayAsync();
        }

        public async Task ScheduleAllNotificationsAsync()
        {
            // Теперь планируем только на сегодня
            await ScheduleNotificationsForTodayAsync();
        }

        public async Task ScheduleNotificationsForTodayAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ScheduleNotificationsForTodayAsync начато ===");

                // Получаем все активные лекарства с расписанием
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicineList = medicines.ToList();

                System.Diagnostics.Debug.WriteLine($"Найдено лекарств с расписанием: {medicineList.Count}");

                // Планируем ТОЛЬКО НА СЕГОДНЯ
                var date = DateTime.Today;
                await ScheduleNotificationsForDateAsync(date, medicineList);

                System.Diagnostics.Debug.WriteLine("=== ScheduleNotificationsForTodayAsync завершено ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка планирования уведомлений: {ex.Message}");
            }
        }

        private async Task ScheduleNotificationsForDateAsync(DateTime date, List<MedicineWithScheduleDto> medicines)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            System.Diagnostics.Debug.WriteLine($"=== Планирование для даты: {dateStr} ===");

            foreach (var medicine in medicines)
            {
                if (!medicine.ScheduleIsActive)
                {
                    System.Diagnostics.Debug.WriteLine($"Лекарство {medicine.MedicineName} не активно");
                    continue;
                }

                var times = ParseTimes(medicine.Times);
                System.Diagnostics.Debug.WriteLine($"Лекарство: {medicine.MedicineName}, Времена: {string.Join(", ", times)}");

                // Проверяем, нужно ли принимать лекарство сегодня
                var shouldTake = ShouldTakeMedicine(medicine, date);

                System.Diagnostics.Debug.WriteLine($"Принимать {medicine.MedicineName} {dateStr}: {shouldTake}");

                if (!shouldTake)
                {
                    System.Diagnostics.Debug.WriteLine($"Пропускаем {medicine.MedicineName} - не нужно принимать сегодня");
                    continue;
                }

                foreach (var time in times)
                {
                    try
                    {
                        var notificationTime = DateTime.Parse($"{dateStr} {time}");

                        System.Diagnostics.Debug.WriteLine($"  Время: {time}, Полное время: {notificationTime}");

                        // Планируем уведомление
                        await ScheduleNotificationAsync(medicine, notificationTime);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка парсинга времени {time}: {ex.Message}");
                    }
                }
            }
        }

        public async Task CancelNotificationsForMedicineAsync(int medicineId)
        {
#if ANDROID
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Отмена уведомлений для лекарства ID: {medicineId} ===");

                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;

                if (alarmManager == null) return;

                // 1. Получаем все записи приема для этого лекарства на сегодня
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var intakes = await _intakeRepository.GetIntakesByMedicineAndDateAsync(medicineId, today);

                foreach (var intake in intakes)
                {
                    // 2. Для каждой записи вычисляем requestCode
                    var notificationTime = DateTime.Parse($"{today} {intake.Time}");
                    var requestCode = GenerateNotificationId(medicineId, notificationTime);

                    // 3. Отменяем уведомление
                    var intent = new Intent(context,
                        Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));

                    intent.SetAction("ACTION_SHOW_NOTIFICATION");

                    var pendingIntent = PendingIntent.GetBroadcast(
                        context,
                        requestCode,
                        intent,
                        PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate);

                    if (pendingIntent != null)
                    {
                        alarmManager.Cancel(pendingIntent);
                        pendingIntent.Cancel();
                        System.Diagnostics.Debug.WriteLine($"✅ Уведомление отменено для intake {intake.IdIntake}, requestCode: {requestCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка отмены уведомлений: {ex.Message}");
            }
#endif
            await Task.CompletedTask;
        }

        public async Task ScheduleNotificationsForMedicineAsync(int medicineId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Планирование уведомлений для лекарства ID: {medicineId} ===");

                // Получаем лекарство с расписанием
                var medicines = await _medicineRepository.GetActiveMedicinesWithSchedulesAsync();
                var medicine = medicines.FirstOrDefault(m => m.IdMedicine == medicineId);

                if (medicine == null || !medicine.ScheduleIsActive)
                {
                    System.Diagnostics.Debug.WriteLine($"Лекарство ID:{medicineId} не активно или не найдено");
                    return;
                }

                var today = DateTime.Today;
                var medicineList = new List<MedicineWithScheduleDto> { medicine };

                await ScheduleNotificationsForDateAsync(today, medicineList);

                System.Diagnostics.Debug.WriteLine($"✅ Уведомления запланированы для лекарства {medicineId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования уведомлений: {ex.Message}");
            }
        }
        private bool ShouldTakeMedicine(MedicineWithScheduleDto medicine, DateTime date)
        {
            var dateStr = date.ToString("yyyy-MM-dd");

            if (!medicine.ScheduleIsActive)
            {
                return false;
            }

            if (medicine.ScheduleTypeCode == "ONETIME")
            {
                return medicine.OneTimeDate == dateStr;
            }
            else if (medicine.ScheduleTypeCode == "RECURRING")
            {
                // Проверяем период действия
                if (!string.IsNullOrEmpty(medicine.DateStart))
                {
                    var startDate = DateTime.Parse(medicine.DateStart);
                    if (date < startDate)
                        return false;
                }

                if (!string.IsNullOrEmpty(medicine.DateEnd))
                {
                    var endDate = DateTime.Parse(medicine.DateEnd);
                    if (date > endDate)
                        return false;
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
                        return false;
                    }

                    var daysDiff = (date - referenceDate).Days;
                    return daysDiff % medicine.DaysInterval.Value == 0;
                }
                else if (medicine.ScheduleModeCode == "WEEKDAYS" && !string.IsNullOrEmpty(medicine.WeekDayIds))
                {
                    var dayNumber = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                    var dayIds = medicine.WeekDayIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.TryParse(id, out var num) ? num : 0)
                        .Where(id => id > 0);

                    return dayIds.Contains(dayNumber);
                }

                // Если режим не указан, принимается каждый день
                return true;
            }

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
        System.Diagnostics.Debug.WriteLine($"=== ScheduleNotificationAsync для {medicine.MedicineName} ===");
        
        var context = Android.App.Application.Context;

        // 1. Получаем или создаем запись о приеме
        var intakeId = await GetOrCreateIntakeIdAsync(medicine, notificationTime);

        if (intakeId == 0)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Не удалось получить intake_id для лекарства: {medicine.MedicineName}");
            return;
        }

        // 2. Проверяем, не было ли уже уведомления
        if (WasNotificationShown(context, intakeId))
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Уведомление уже было показано для intake_id: {intakeId}");
            return;
        }

        // 3. Проверка времени
        var timeDifference = notificationTime - DateTime.Now;

        if (timeDifference.TotalHours < -6)
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Время приема {medicine.MedicineName} прошло более 6 часов назад, пропускаем");
            return;
        }
        else if (timeDifference.TotalSeconds < 0)
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Время {medicine.MedicineName} только что прошло ({timeDifference.TotalMinutes:0} минут), планируем через 30 секунд");
            notificationTime = DateTime.Now.AddSeconds(30);
        }

        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;

        if (alarmManager == null)
        {
            System.Diagnostics.Debug.WriteLine($"❌ AlarmManager не найден!");
            return;
        }

        // 4. ✅ ДОБАВЛЯЕМ unit_name в Intent
        var intent = new Intent(context,
            Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));

        intent.SetAction("ACTION_SHOW_NOTIFICATION");
        intent.PutExtra("medicine_id", medicine.IdMedicine);
        intent.PutExtra("medicine_name", medicine.MedicineName);
        intent.PutExtra("dosage", medicine.Dosage);
        intent.PutExtra("recipient_name", medicine.RecipientName);
        intent.PutExtra("intake_id", intakeId);
        intent.PutExtra("unit_name", medicine.UnitName); // ✅ Добавляем единицу измерения
        intent.PutExtra("scheduled_time", notificationTime.ToString("HH:mm"));

        // Уникальный ID для каждого уведомления
        var requestCode = GenerateNotificationId(medicine.IdMedicine, notificationTime);

        System.Diagnostics.Debug.WriteLine($"Создаем уведомление для: {medicine.MedicineName}");
        System.Diagnostics.Debug.WriteLine($"Дозировка: {medicine.Dosage} {medicine.UnitName}");
        System.Diagnostics.Debug.WriteLine($"Время: {notificationTime:HH:mm}");
        System.Diagnostics.Debug.WriteLine($"RequestCode: {requestCode}");
        System.Diagnostics.Debug.WriteLine($"IntakeId: {intakeId}");

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            requestCode,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        // 5. Конвертируем время в миллисекунды
        var triggerTime = GetDateTimeInMillis(notificationTime);
        var nowMillis = GetDateTimeInMillis(DateTime.Now);

        System.Diagnostics.Debug.WriteLine($"Триггерное время (мс): {triggerTime}");
        System.Diagnostics.Debug.WriteLine($"Текущее время (мс): {nowMillis}");
        System.Diagnostics.Debug.WriteLine($"Разница (сек): {(triggerTime - nowMillis) / 1000}");

        // 6. Планируем уведомление
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            alarmManager.SetExactAndAllowWhileIdle(
                AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);

            System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExactAndAllowWhileIdle");
        }
        else
        {
            alarmManager.SetExact(
                AlarmType.RtcWakeup,
                triggerTime,
                pendingIntent);

            System.Diagnostics.Debug.WriteLine($"✅ Уведомление запланировано с setExact");
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Ошибка планирования: {ex.Message}");
    }
#endif
            await Task.CompletedTask;
        }

#if ANDROID
        private async Task<int> GetOrCreateIntakeIdAsync(MedicineWithScheduleDto medicine, DateTime notificationTime)
        {
            try
            {
                var dateStr = notificationTime.ToString("yyyy-MM-dd");
                var timeStr = notificationTime.ToString("HH:mm");

                // 1. Сначала проверяем, есть ли уже запись в Intake
                var existingIntake = await _intakeRepository.GetIntakeByMedicineAndDateTimeAsync(
                    medicine.IdMedicine,
                    dateStr,
                    timeStr);

                if (existingIntake != null)
                {
                    // 2. Если запись уже существует, возвращаем ее ID
                    System.Diagnostics.Debug.WriteLine($"✅ Запись Intake уже существует: ID={existingIntake.IdIntake}");
                    return existingIntake.IdIntake;
                }

                // 3. Только если записи нет - создаем новую
                System.Diagnostics.Debug.WriteLine($"🆕 Создаем новую запись Intake для лекарства: {medicine.MedicineName}");
        
                // Получаем ID ScheduleTime
                var scheduleTime = await _scheduleTimeRepository.GetScheduleTimeByScheduleAndTimeAsync(
                    medicine.IdSchedule, timeStr);
                
                if (scheduleTime == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Не найден ScheduleTime для времени {timeStr}");
                    return 0;
                }
        
                var intakeModel = new Models.IntakeModel
                {
                    IdMedicine = medicine.IdMedicine,
                    IdSchedule = medicine.IdSchedule,
                    IdScheduleTime = scheduleTime.IdTime,
                    Date = dateStr,
                    Time = timeStr,
                    ActualDosage = medicine.Dosage,
                    IsCompleted = false,
                    TakenDateTime = null
                };

                return await _intakeRepository.AddIntakeAsync(intakeModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка получения/создания intake_id: {ex.Message}");
                return 0;
            }
        }

        private bool WasNotificationShown(Context context, int intakeId)
        {
            try
            {
                var prefs = context.GetSharedPreferences("notifications", FileCreationMode.Private);
                return prefs.GetBoolean($"shown_{intakeId}", false);
            }
            catch
            {
                return false;
            }
        }

        private long GetDateTimeInMillis(DateTime dateTime)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(utcTime - epoch).TotalMilliseconds;
        }
#endif

        private int GenerateNotificationId(int medicineId, DateTime time)
        {
            return Math.Abs($"{medicineId}{time:yyyyMMddHHmm}".GetHashCode());
        }

        public void CancelAllNotifications()
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;

                if (alarmManager == null)
                    return;

                // Создаем Intent с тем же типом для отмены
                var intent = new Intent(context, 
                    Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));
                
                intent.SetAction("ACTION_SHOW_NOTIFICATION");
                
                // Отменяем все уведомления для сегодняшнего дня
                for (int i = 0; i < 1000; i++) // Максимальный ID для отмены
                {
                    var pendingIntent = PendingIntent.GetBroadcast(
                        context,
                        i,
                        intent,
                        PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate);

                    if (pendingIntent != null)
                    {
                        alarmManager.Cancel(pendingIntent);
                        pendingIntent.Cancel();
                        System.Diagnostics.Debug.WriteLine($"✅ Уведомление {i} отменено");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка отмены уведомлений: {ex.Message}");
            }
#endif
        }

        public async Task CancelNotificationForIntakeAsync(int intakeId)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
                
                if (alarmManager == null) return;
                
                // Создаем Intent с тем же action
                var intent = new Intent(context, 
                    Java.Lang.Class.FromType(typeof(MedicinesTracker.Platforms.Android.NotificationPublisher)));
                
                intent.SetAction("ACTION_SHOW_NOTIFICATION");
                
                // Используем intakeId как requestCode для поиска
                var pendingIntent = PendingIntent.GetBroadcast(
                    context,
                    intakeId,
                    intent,
                    PendingIntentFlags.Immutable | PendingIntentFlags.NoCreate);
                
                if (pendingIntent != null)
                {
                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                    System.Diagnostics.Debug.WriteLine($"✅ Уведомление для intake {intakeId} отменено");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка отмены уведомления: {ex.Message}");
            }
#endif
            await Task.CompletedTask;
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