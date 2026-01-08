using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IDatabaseService
    {
        Task<SqliteConnection> GetConnectionAsync();
        string GetDatabasePath();
    }

    public class DatabaseService : IDatabaseService
    {
        private const string DatabaseFileName = "MedicineTracker.db";
        private string _databasePath;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DatabaseService()
        {
            _databasePath = GetDatabasePath();
            Debug.WriteLine($"Database path: {_databasePath}");
        }

        public string GetDatabasePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        }

        public async Task<SqliteConnection> GetConnectionAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    await InitializeDatabaseAsync();
                    _isInitialized = true;
                }

                var connection = new SqliteConnection($"Data Source={_databasePath};");
                await connection.OpenAsync();

                // Включаем поддержку внешних ключей для каждого соединения
                using var command = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                await command.ExecuteNonQueryAsync();

                return connection;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                bool needsRecreation = false;

                // Если БД существует - проверяем её структуру
                if (File.Exists(_databasePath))
                {
                    Debug.WriteLine("Database exists, checking structure...");

                    // Проверяем, есть ли основные таблицы
                    if (await IsDatabaseValidAsync())
                    {
                        Debug.WriteLine("Database is valid, using it.");
                        return;
                    }
                    else
                    {
                        Debug.WriteLine("Database exists but is invalid or empty. Will recreate.");
                        needsRecreation = true;
                    }
                }
                else
                {
                    Debug.WriteLine("Database not found, creating new...");
                }

                // Удаляем старую БД если она невалидна
                if (needsRecreation)
                {
                    try
                    {
                        File.Delete(_databasePath);
                        Debug.WriteLine("Old invalid database deleted.");
                    }
                    catch (Exception deleteEx)
                    {
                        Debug.WriteLine($"Warning: Could not delete old database: {deleteEx.Message}");
                    }
                }

                // Создаем новую БД с таблицами и данными
                await CreateAndInitializeDatabaseAsync();

                Debug.WriteLine("Database created and initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> IsDatabaseValidAsync()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath};");
                await connection.OpenAsync();

                // Включаем foreign keys для проверки
                using var fkCommand = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
                await fkCommand.ExecuteNonQueryAsync();

                // Проверяем наличие основных таблиц
                var checkTablesSql = @"
                    SELECT COUNT(*) FROM sqlite_master 
                    WHERE type='table' AND name IN 
                    ('Medicine', 'Unit', 'MethodAdmission', 'Recipient', 'Stock', 'Intake', 
                    'MedicationSchedule', 'NotificationSetting', 'RecurrencePattern', 'ScheduleMode',
                    'ScheduleTime', 'ScheduleType', 'ScheduleWeekDays', 'WeekDay')";

                using var command = new SqliteCommand(checkTablesSql, connection);
                var result = Convert.ToInt32(await command.ExecuteScalarAsync());

                // Должно быть минимум 15 основных таблиц
                return result >= 14;
            }
            catch
            {
                // Если произошла ошибка при проверке, считаем БД невалидной
                return false;
            }
        }

        private async Task CreateAndInitializeDatabaseAsync()
        {
            // Создаем директорию для БД, если её нет
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var connection = new SqliteConnection($"Data Source={_databasePath};");
            await connection.OpenAsync();

            // Включаем поддержку внешних ключей
            using var fkCommand = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            await fkCommand.ExecuteNonQueryAsync();

            // Создаем все таблицы с каскадным удалением
            await CreateTablesAsync(connection);

            // Заполняем справочные данные
            await InsertInitialDataAsync(connection);
        }

        private async Task CreateTablesAsync(SqliteConnection connection)
        {
            var createTablesSql = @"
                -- Справочные таблицы (без внешних ключей)
                CREATE TABLE IF NOT EXISTS Unit (
                    IdUnit INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS MethodAdmission (
                    IdMethodAdmission INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Recipient (
                    IdRecipient INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS RecurrencePattern (
                    IdPattern INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    DaysInterval INTEGER NOT NULL,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS ScheduleType (
                    IdType INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS ScheduleMode (
                    IdMode INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS WeekDay (
                    IdDay INTEGER PRIMARY KEY AUTOINCREMENT,
                    Number INTEGER NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    ShortName TEXT
                );

                -- Основные таблицы с каскадным удалением
                CREATE TABLE IF NOT EXISTS Medicine (
                    IdMedicine INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    IdUnit INTEGER NOT NULL,
                    IdMethodAdmission INTEGER NOT NULL,
                    IdRecipient INTEGER NOT NULL,
                    FOREIGN KEY (IdUnit) REFERENCES Unit(IdUnit) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdMethodAdmission) REFERENCES MethodAdmission(IdMethodAdmission) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdRecipient) REFERENCES Recipient(IdRecipient) ON DELETE CASCADE ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Stock (
                    IdStock INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdMedicine INTEGER NOT NULL UNIQUE,
                    Threshold INTEGER NOT NULL DEFAULT 10,
                    CurrentQuantity INTEGER NOT NULL DEFAULT 0,
                    ReminderEnabled BOOLEAN NOT NULL DEFAULT TRUE,
                    FOREIGN KEY (IdMedicine) REFERENCES Medicine(IdMedicine) ON DELETE CASCADE ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS NotificationSetting (
                    IdNotificationSetting INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdRecipient INTEGER NOT NULL,
                    IsEnabled BOOLEAN NOT NULL DEFAULT TRUE,
                    Sound TEXT NOT NULL DEFAULT 'default',
                    FOREIGN KEY (IdRecipient) REFERENCES Recipient(IdRecipient) ON DELETE CASCADE ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS MedicationSchedule (
                    IdSchedule INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdMedicine INTEGER NOT NULL,
                    IdScheduleType INTEGER NOT NULL,
                    IdScheduleMode INTEGER,
                    IdRecurrencePattern INTEGER,
                    OneTimeDate TEXT,
                    Dosage INTEGER NOT NULL DEFAULT 1,
                    DateStart TEXT,
                    DateEnd TEXT,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    FOREIGN KEY (IdMedicine) REFERENCES Medicine(IdMedicine) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdScheduleType) REFERENCES ScheduleType(IdType) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdScheduleMode) REFERENCES ScheduleMode(IdMode) ON DELETE SET NULL ON UPDATE CASCADE,
                    FOREIGN KEY (IdRecurrencePattern) REFERENCES RecurrencePattern(IdPattern) ON DELETE SET NULL ON UPDATE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ScheduleWeekDays (
                    IdLink INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdSchedule INTEGER NOT NULL,
                    IdDay INTEGER NOT NULL,
                    FOREIGN KEY (IdSchedule) REFERENCES MedicationSchedule(IdSchedule) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdDay) REFERENCES WeekDay(IdDay) ON DELETE CASCADE ON UPDATE CASCADE,
                    UNIQUE(IdSchedule, IdDay)
                );

                CREATE TABLE IF NOT EXISTS ScheduleTime (
                    IdTime INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdSchedule INTEGER NOT NULL,
                    Time TEXT NOT NULL,
                    OrderInDay INTEGER NOT NULL DEFAULT 1,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    FOREIGN KEY (IdSchedule) REFERENCES MedicationSchedule(IdSchedule) ON DELETE CASCADE ON UPDATE CASCADE,
                    UNIQUE(IdSchedule, Time)
                );

                CREATE TABLE IF NOT EXISTS Intake (
                    IdIntake INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdMedicine INTEGER NOT NULL,
                    IsCompleted BOOLEAN NOT NULL DEFAULT TRUE,
                    IdSchedule INTEGER,
                    IdScheduleTime INTEGER,
                    Date TEXT NOT NULL,
                    Time TEXT NOT NULL,
                    ActualDosage INTEGER,
                    Status INTEGER,
                    FOREIGN KEY (IdMedicine) REFERENCES Medicine(IdMedicine) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdSchedule) REFERENCES MedicationSchedule(IdSchedule) ON DELETE SET NULL ON UPDATE CASCADE,
                    FOREIGN KEY (IdScheduleTime) REFERENCES ScheduleTime(IdTime) ON DELETE SET NULL ON UPDATE CASCADE
                );

                -- Триггер для обновления статуса при вставке/обновлении
                CREATE TRIGGER UpdateIntakeStatus
                AFTER INSERT ON Intake
                BEGIN
                    UPDATE Intake 
                    SET Status = CASE 
                        WHEN IsCompleted = 1 THEN 'Принято'
                        WHEN Date < date('now') THEN 'Пропущено'
                        WHEN Date = date('now') AND Time < time('now') THEN 'Пропущено'
                        WHEN Date = date('now') AND Time >= time('now') THEN 'Ожидает'
                        ELSE 'Запланировано'
                    END
                    WHERE IdIntake = NEW.IdIntake;
                END;";

            using var command = new SqliteCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine("Tables created successfully with cascade delete.");
        }

        private async Task InsertInitialDataAsync(SqliteConnection connection)
        {
            try
            {
                Debug.WriteLine("Inserting initial data...");

                // Разделяем вставку на отдельные команды для лучшей отладки
                var commands = new[]
                {
                    // Units
                    @"INSERT OR IGNORE INTO Unit (Name) VALUES 
                    ('таблетка(и)'), ('капсула(ы)'), ('мл'), ('г'), ('чайная ложка(и)')",
                    
                    // MethodAdmission
                    @"INSERT OR IGNORE INTO MethodAdmission (Name) VALUES 
                    ('Перорально'), ('Инъекционно'), ('Наружно'), ('Ингаляционно')",
                    
                    // WeekDays
                    @"INSERT OR IGNORE INTO WeekDay (Number, Name, ShortName) VALUES
                    (1, 'Понедельник', 'Пн'),
                    (2, 'Вторник', 'Вт'),
                    (3, 'Среда', 'Ср'),
                    (4, 'Четверг', 'Чт'),
                    (5, 'Пятница', 'Пт'),
                    (6, 'Суббота', 'Сб'),
                    (7, 'Воскресенье', 'Вс')",
                    
                    // RecurrencePatterns
                    @"INSERT OR IGNORE INTO RecurrencePattern (Name, DaysInterval, Description) VALUES 
                    ('Раз в неделю', 7, 'Один раз в неделю'),
                    ('Раз в 3 дня', 3, 'Через два дня'),
                    ('Раз в 2 дня', 2, 'Через день'),
                    ('Каждый день', 1, 'Ежедневно')",
                    
                    // ScheduleModes
                    @"INSERT OR IGNORE INTO ScheduleMode (Code, Name, Description) VALUES 
                    ('INTERVAL', 'Интервальное', 'Прием через равные промежутки времени'),
                    ('WEEKDAYS', 'По дням недели', 'Прием в конкретные дни недели')",
                    
                    // ScheduleTypes
                    @"INSERT OR IGNORE INTO ScheduleType (Code, Name, Description) VALUES 
                    ('RECURRING', 'Повторяющееся', 'Регулярный приём по расписанию'),
                    ('ONETIME', 'Одноразовое', 'Один раз в указанную дату')",
                    
                };

                int totalRowsAffected = 0;
                for (int i = 0; i < commands.Length; i++)
                {
                    try
                    {
                        using var command = new SqliteCommand(commands[i], connection);
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        totalRowsAffected += rowsAffected;
                        Debug.WriteLine($"Command {i + 1} executed: {rowsAffected} rows affected");
                    }
                    catch (Exception cmdEx)
                    {
                        Debug.WriteLine($"Error executing command {i + 1}: {cmdEx.Message}");
                    }
                }

                Debug.WriteLine($"Total initial data inserted: {totalRowsAffected} rows");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting initial data: {ex.Message}");
            }
        }
    }
}