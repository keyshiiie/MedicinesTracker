// Services/ConfigurationService.cs
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace MedicinesTracker.Services
{
    public interface IConfigurationService
    {
        string GetDatabasePath();
        string GetConnectionString();
    }

    public class ConfigurationService : IConfigurationService
    {
        public string GetDatabasePath()
        {
            var databasePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "MedicineTracker.db");

            Debug.WriteLine($"[ConfigurationService] Database path: {databasePath}");
            Debug.WriteLine($"[ConfigurationService] File exists: {File.Exists(databasePath)}");

            return databasePath;
        }

        public string GetConnectionString()
        {
            var dbPath = GetDatabasePath();
            return $"Data Source={dbPath}";
        }
    }
}