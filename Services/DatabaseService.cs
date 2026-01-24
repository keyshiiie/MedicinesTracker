using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IDatabaseService
    {
        Task<SqliteConnection> GetOpenConnectionAsync();
        Task<SqliteConnection> GetConnectionAsync();
        string GetDatabasePath();
        Task EnsureInitializedAsync();
        Task ForceRecreateDatabaseAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private const string DatabaseFileName = "MedicineTracker.db";
        private string _databasePath;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private static readonly SemaphoreSlim _globalInitSemaphore = new(1, 1);
        private static bool _globalInitialized = false;

        public DatabaseService()
        {
            _databasePath = GetDatabasePath();
            Debug.WriteLine($"Database path: {_databasePath}");
        }

        public string GetDatabasePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        }

        // Получить уже открытое соединение
        public async Task<SqliteConnection> GetOpenConnectionAsync()
        {
            await EnsureInitializedAsync();

            var connection = new SqliteConnection($"Data Source={_databasePath};");
            await connection.OpenAsync();

            // Включаем foreign keys для этого соединения
            using var command = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            await command.ExecuteNonQueryAsync();

            return connection;
        }

        // Альтернативный метод для ручного управления
        public async Task<SqliteConnection> GetConnectionAsync()
        {
            await EnsureInitializedAsync();
            return new SqliteConnection($"Data Source={_databasePath};");
        }

        // Публичный метод для принудительной инициализации
        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            // Блокируем только на время инициализации
            await _initSemaphore.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                // Глобальная блокировка для проверки существования файла
                await _globalInitSemaphore.WaitAsync();
                try
                {
                    if (_globalInitialized)
                    {
                        _isInitialized = true;
                        return;
                    }

                    // Фоновая инициализация
                    await Task.Run(async () =>
                    {
                        await InitializeDatabaseAsync();
                    });

                    _globalInitialized = true;
                }
                finally
                {
                    _globalInitSemaphore.Release();
                }

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                bool needsCreation = !File.Exists(_databasePath);

                if (!needsCreation)
                {
                    // Проверяем существующую БД
                    if (!await IsDatabaseValidAsync())
                    {
                        Debug.WriteLine("Database exists but is invalid. Will recreate.");
                        await SafeDeleteDatabaseAsync();
                        needsCreation = true;
                    }
                    else
                    {
                        Debug.WriteLine("Database is valid, ensuring reference data.");
                        // Загружаем справочные данные в существующую БД
                        await EnsureReferenceDataAsync();
                        return;
                    }
                }

                if (needsCreation)
                {
                    Debug.WriteLine("Creating new database...");
                    await CreateAndInitializeDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureReferenceDataAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath};");
            await connection.OpenAsync();
            await InsertInitialDataAsync(connection);
        }

        private async Task<bool> IsDatabaseValidAsync()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath};");
                await connection.OpenAsync();

                // Проверяем только структуру, не данные
                var checkTablesSql = @"
            SELECT COUNT(*) FROM sqlite_master 
            WHERE type='table' AND name IN ('Unit', 'MethodAdmission', 'Medicine', 'Intake')";

                using var command = new SqliteCommand(checkTablesSql, connection);
                var result = Convert.ToInt32(await command.ExecuteScalarAsync());

                return result >= 4; // Минимум 4 основные таблицы
            }
            catch
            {
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

            // Включаем кэширование для производительности
            using var cacheCommand = new SqliteCommand("PRAGMA cache_size = -2000;", connection);
            await cacheCommand.ExecuteNonQueryAsync();

            // Включаем journal mode для лучшей производительности
            using var journalCommand = new SqliteCommand("PRAGMA journal_mode = WAL;", connection);
            await journalCommand.ExecuteNonQueryAsync();

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
                    IsCompleted BOOLEAN NOT NULL DEFAULT FALSE,
                    IdSchedule INTEGER,
                    IdScheduleTime INTEGER,
                    Date TEXT NOT NULL,
                    Time TEXT NOT NULL,
                    TakenDateTime TEXT,
                    ActualDosage INTEGER,
                    FOREIGN KEY (IdMedicine) REFERENCES Medicine(IdMedicine) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (IdSchedule) REFERENCES MedicationSchedule(IdSchedule) ON DELETE SET NULL ON UPDATE CASCADE,
                    FOREIGN KEY (IdScheduleTime) REFERENCES ScheduleTime(IdTime) ON DELETE SET NULL ON UPDATE CASCADE
                );

                -- индексы
                CREATE INDEX IF NOT EXISTS idx_intake_completed_date ON Intake(IsCompleted, Date);
                CREATE INDEX IF NOT EXISTS idx_intake_taken_date ON Intake(TakenDateTime, Date) 
                WHERE TakenDateTime IS NOT NULL;
                CREATE INDEX IF NOT EXISTS idx_intake_date ON Intake(Date);
                CREATE INDEX IF NOT EXISTS idx_intake_medicine ON Intake(IdMedicine);
                CREATE INDEX IF NOT EXISTS idx_schedule_active ON MedicationSchedule(IsActive);
                CREATE INDEX IF NOT EXISTS idx_intake_date_completed ON Intake(Date, IsCompleted);
                CREATE INDEX IF NOT EXISTS idx_intake_medicine_date ON Intake(IdMedicine, Date);
                CREATE INDEX IF NOT EXISTS idx_intake_schedule ON Intake(IdSchedule, IdScheduleTime);
                CREATE INDEX IF NOT EXISTS idx_medicine_recipient ON Medicine(IdRecipient);
                CREATE INDEX IF NOT EXISTS idx_schedule_medicine ON MedicationSchedule(IdMedicine, IsActive);
                CREATE INDEX IF NOT EXISTS idx_schedule_type ON MedicationSchedule(IdScheduleType);
                CREATE INDEX IF NOT EXISTS idx_stock_medicine ON Stock(IdMedicine);
                CREATE INDEX IF NOT EXISTS idx_intake_datetime ON Intake(Date, Time);

                CREATE UNIQUE INDEX IF NOT EXISTS idx_intake_unique 
                ON Intake(IdMedicine, IdSchedule, IdScheduleTime, Date, Time);";

            using var command = new SqliteCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine("Tables created successfully with cascade delete.");
        }

        private async Task InsertInitialDataAsync(SqliteConnection connection)
        {
            try
            {
                Debug.WriteLine("Inserting/updating initial data...");

                // ВСЕГДА вставляем или обновляем справочные данные
                var commands = new[]
                {
            // Units - всегда добавляем, если нет
            @"INSERT OR IGNORE INTO Unit (Name) VALUES 
            ('таблетка(и)'), ('капсула(ы)'), ('мл'), ('г'), ('чайная ложка(и)')",
            
            // MethodAdmission
            @"INSERT OR IGNORE INTO MethodAdmission (Name) VALUES 
            ('Перорально'), ('Инъекционно'), ('Наружно'), ('Ингаляционно')",
            
            // WeekDays - используем INSERT OR REPLACE чтобы гарантировать наличие
            @"INSERT OR REPLACE INTO WeekDay (Number, Name, ShortName) VALUES
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
                        // Продолжаем даже если одна команда упала
                    }
                }

                Debug.WriteLine($"Total initial data inserted/updated: {totalRowsAffected} rows");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inserting initial data: {ex.Message}");
                // Не бросаем исключение - лучше пустая БД чем сбой приложения
            }
        }

        private async Task SafeDeleteDatabaseAsync()
        {
            try
            {
                // Закрываем все соединения
                SqliteConnection.ClearAllPools();
                await Task.Delay(100);

                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                    Debug.WriteLine("Database deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting database: {ex.Message}");
                throw;
            }
        }

        public async Task ForceRecreateDatabaseAsync()
        {
            // Сбрасываем только локальные флаги
            await _initSemaphore.WaitAsync();
            try
            {
                _isInitialized = false;

                // Сбрасываем глобальный флаг под защитой семафора
                await _globalInitSemaphore.WaitAsync();
                try
                {
                    _globalInitialized = false;

                    await SafeDeleteDatabaseAsync();
                    await InitializeDatabaseAsync();

                    // После успешного пересоздания устанавливаем флаги
                    _globalInitialized = true;
                }
                finally
                {
                    _globalInitSemaphore.Release();
                }

                _isInitialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }
    }
}