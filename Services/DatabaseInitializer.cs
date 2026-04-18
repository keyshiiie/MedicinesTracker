using Microsoft.EntityFrameworkCore;
using MedicinesTracker.Data;

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
        }

        public async Task ForceRecreateAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            await ConfigurePragmas();
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