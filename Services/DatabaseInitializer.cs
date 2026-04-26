using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Services
{
    public interface IDatabaseInitializer
    {
        Task EnsureCreatedAsync();
        Task ForceRecreateAsync();
    }

    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly AppDbContext _context;

        public DatabaseInitializer(AppDbContext context)
        {
            _context = context;
        }

        public async Task EnsureCreatedAsync()
        {
            // Создаст БД и все таблицы, применит Seed Data
            await _context.Database.EnsureCreatedAsync();

            // Настройка PRAGMA (опционально)
            await ConfigurePragmas();

            await SeedFirstLaunchFlagIfNeeded();
        }

        public async Task ForceRecreateAsync()
        {
            // 1. Закрываем все соединения
            await _context.Database.CloseConnectionAsync();

            // 2. Очищаем контекст
            _context.ChangeTracker.Clear();

            // 3. Удаляем через EnsureDeletedAsync
            await _context.Database.EnsureDeletedAsync();

            // 4. ДОПОЛНИТЕЛЬНО: принудительно удаляем файл (на случай, если EnsureDeletedAsync не сработал)
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MedicineTracker.db");
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                    System.Diagnostics.Debug.WriteLine("🗑️ Файл БД удалён принудительно");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Не удалось удалить файл: {ex.Message}");
                }
            }

            // 5. Создаём заново
            await _context.Database.EnsureCreatedAsync();
            await ConfigurePragmas();
            await SeedFirstLaunchFlagIfNeeded();

            System.Diagnostics.Debug.WriteLine("✅ ForceRecreateAsync завершён успешно");
        }

        private async Task SeedFirstLaunchFlagIfNeeded()
        {
            // Проверяем, существует ли запись о первом запуске
            var firstLaunchFlag = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "FirstLaunchCompleted");

            if (firstLaunchFlag == null)
            {
                // При первом создании БД добавляем флаг = false
                _context.AppSettings.Add(new AppSetting
                {
                    Key = "FirstLaunchCompleted",
                    Value = "false"
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task ConfigurePragmas()
        {
            // Если нужно принудительно включить PRAGMA
            await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
            await _context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;");
            await _context.Database.ExecuteSqlRawAsync("PRAGMA cache_size = -2000;");
        }
    }
}