using Microsoft.EntityFrameworkCore;
using MedicinesTracker.Entities;

namespace MedicinesTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet для всех сущностей
        public DbSet<Intake> Intakes { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<MedicationSchedule> MedicationSchedules { get; set; }
        public DbSet<MethodAdmission> MethodAdmissions { get; set; }
        public DbSet<Recipient> Recipients { get; set; }
        public DbSet<RecurrencePattern> RecurrencePatterns { get; set; }
        public DbSet<ScheduleMode> ScheduleModes { get; set; }
        public DbSet<ScheduleTime> ScheduleTimes { get; set; }
        public DbSet<ScheduleType> ScheduleTypes { get; set; }
        public DbSet<ScheduleWeekDay> ScheduleWeekDays { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<WeekDay> WeekDays { get; set; }
        public DbSet<NotificationSetting> NotificationSettings { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== НАСТРОЙКА ИНДЕКСОВ ==========

            // Индексы для Intake
            modelBuilder.Entity<Intake>(entity =>
            {
                entity.HasIndex(e => new { e.IsCompleted, e.Date })
                      .HasDatabaseName("idx_intake_completed_date");

                entity.HasIndex(e => new { e.Date, e.IsCompleted })
                      .HasDatabaseName("idx_intake_date_completed");

                entity.HasIndex(e => e.Date)
                      .HasDatabaseName("idx_intake_date");

                entity.HasIndex(e => e.IdMedicine)
                      .HasDatabaseName("idx_intake_medicine");

                entity.HasIndex(e => new { e.IdMedicine, e.Date })
                      .HasDatabaseName("idx_intake_medicine_date");

                entity.HasIndex(e => new { e.IdSchedule, e.IdScheduleTime })
                      .HasDatabaseName("idx_intake_schedule");

                // Уникальное ограничение (предотвращает дубликаты)
                entity.HasIndex(e => new { e.IdMedicine, e.IdSchedule, e.IdScheduleTime, e.Date, e.Time })
                      .IsUnique()
                      .HasDatabaseName("idx_intake_unique");

                entity.HasIndex(e => new { e.Date, e.Time })
                      .HasDatabaseName("idx_intake_datetime");
            });

            // Индексы для Medicine
            modelBuilder.Entity<Medicine>(entity =>
            {
                entity.HasIndex(e => e.IdRecipient)
                      .HasDatabaseName("idx_medicine_recipient");
            });

            // Индексы для MedicationSchedule
            modelBuilder.Entity<MedicationSchedule>(entity =>
            {
                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("idx_schedule_active");

                entity.HasIndex(e => new { e.IdMedicine, e.IsActive })
                      .HasDatabaseName("idx_schedule_medicine");

                entity.HasIndex(e => e.IdScheduleType)
                      .HasDatabaseName("idx_schedule_type");
            });

            // Индекс для Stock
            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasIndex(e => e.IdMedicine)
                      .HasDatabaseName("idx_stock_medicine");
            });

            // ========== НАСТРОЙКА КАСКАДНОГО УДАЛЕНИЯ ==========

            // По умолчанию все внешние ключи - CASCADE
            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
            }

            // Исключения (ON DELETE SET NULL)
            modelBuilder.Entity<MedicationSchedule>()
                .HasOne(m => m.ScheduleMode)
                .WithMany(s => s.Schedules)
                .HasForeignKey(m => m.IdScheduleMode)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MedicationSchedule>()
                .HasOne(m => m.RecurrencePattern)
                .WithMany(r => r.Schedules)
                .HasForeignKey(m => m.IdRecurrencePattern)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Intake>()
                .HasOne(i => i.Schedule)
                .WithMany(s => s.Intakes)
                .HasForeignKey(i => i.IdSchedule)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Intake>()
                .HasOne(i => i.ScheduleTime)
                .WithMany(st => st.Intakes)
                .HasForeignKey(i => i.IdScheduleTime)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== УНИКАЛЬНЫЕ ОГРАНИЧЕНИЯ ==========

            modelBuilder.Entity<ScheduleTime>(entity =>
            {
                entity.HasIndex(e => new { e.IdSchedule, e.Time })
                      .IsUnique()
                      .HasDatabaseName("idx_schedule_time_unique");
            });

            modelBuilder.Entity<ScheduleWeekDay>(entity =>
            {
                entity.HasIndex(e => new { e.IdSchedule, e.IdDay })
                      .IsUnique()
                      .HasDatabaseName("idx_schedule_weekday_unique");
            });

            modelBuilder.Entity<Unit>(entity =>
            {
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("idx_unit_name_unique");
            });

            modelBuilder.Entity<MethodAdmission>(entity =>
            {
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("idx_method_admission_name_unique");
            });

            modelBuilder.Entity<RecurrencePattern>(entity =>
            {
                entity.HasIndex(e => e.Name)
                      .IsUnique()
                      .HasDatabaseName("idx_recurrence_pattern_name_unique");
            });

            modelBuilder.Entity<ScheduleMode>(entity =>
            {
                entity.HasIndex(e => e.Code)
                      .IsUnique()
                      .HasDatabaseName("idx_schedule_mode_code_unique");
            });

            modelBuilder.Entity<ScheduleType>(entity =>
            {
                entity.HasIndex(e => e.Code)
                      .IsUnique()
                      .HasDatabaseName("idx_schedule_type_code_unique");
            });

            modelBuilder.Entity<WeekDay>(entity =>
            {
                entity.HasIndex(e => e.Number)
                      .IsUnique()
                      .HasDatabaseName("idx_weekday_number_unique");
            });

            modelBuilder.Entity<Stock>(entity =>
            {
                entity.HasIndex(e => e.IdMedicine)
                      .IsUnique()
                      .HasDatabaseName("idx_stock_medicine_unique");
            });

            // ========== SEED DATA (НАЧАЛЬНЫЕ ДАННЫЕ) ==========

            // Единицы измерения
            modelBuilder.Entity<Unit>().HasData(
                new Unit { IdUnit = 1, Name = "таблетка(и)" },
                new Unit { IdUnit = 2, Name = "капсула(ы)" },
                new Unit { IdUnit = 3, Name = "мл" },
                new Unit { IdUnit = 4, Name = "г" },
                new Unit { IdUnit = 5, Name = "чайная ложка(и)" }
            );

            // Способы применения
            modelBuilder.Entity<MethodAdmission>().HasData(
                new MethodAdmission { IdMethodAdmission = 1, Name = "Перорально" },
                new MethodAdmission { IdMethodAdmission = 2, Name = "Инъекционно" },
                new MethodAdmission { IdMethodAdmission = 3, Name = "Наружно" },
                new MethodAdmission { IdMethodAdmission = 4, Name = "Ингаляционно" }
            );

            // Дни недели
            modelBuilder.Entity<WeekDay>().HasData(
                new WeekDay { IdDay = 1, Number = 1, Name = "Понедельник", ShortName = "Пн" },
                new WeekDay { IdDay = 2, Number = 2, Name = "Вторник", ShortName = "Вт" },
                new WeekDay { IdDay = 3, Number = 3, Name = "Среда", ShortName = "Ср" },
                new WeekDay { IdDay = 4, Number = 4, Name = "Четверг", ShortName = "Чт" },
                new WeekDay { IdDay = 5, Number = 5, Name = "Пятница", ShortName = "Пт" },
                new WeekDay { IdDay = 6, Number = 6, Name = "Суббота", ShortName = "Сб" },
                new WeekDay { IdDay = 7, Number = 7, Name = "Воскресенье", ShortName = "Вс" }
            );

            // Паттерны повторения
            modelBuilder.Entity<RecurrencePattern>().HasData(
                new RecurrencePattern { IdPattern = 1, Name = "Раз в неделю", DaysInterval = 7, Description = "Один раз в неделю" },
                new RecurrencePattern { IdPattern = 2, Name = "Раз в 3 дня", DaysInterval = 3, Description = "Через два дня" },
                new RecurrencePattern { IdPattern = 3, Name = "Раз в 2 дня", DaysInterval = 2, Description = "Через день" },
                new RecurrencePattern { IdPattern = 4, Name = "Каждый день", DaysInterval = 1, Description = "Ежедневно" }
            );

            // Режимы расписания
            modelBuilder.Entity<ScheduleMode>().HasData(
                new ScheduleMode { IdMode = 1, Code = "INTERVAL", Name = "Интервальное", Description = "Прием через равные промежутки времени" },
                new ScheduleMode { IdMode = 2, Code = "WEEKDAYS", Name = "По дням недели", Description = "Прием в конкретные дни недели" }
            );

            // Типы расписания
            modelBuilder.Entity<ScheduleType>().HasData(
                new ScheduleType { IdType = 1, Code = "RECURRING", Name = "Повторяющееся", Description = "Регулярный приём по расписанию" },
                new ScheduleType { IdType = 2, Code = "ONETIME", Name = "Одноразовое", Description = "Один раз в указанную дату" }
            );
        }
    }
}