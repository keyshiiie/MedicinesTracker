using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace MedicinesTracker.DataAccess
{
    public class DBConnection : IDisposable
    {
        private readonly string _databasePath;
        private bool _disposed;


        public DBConnection()
        {
            _databasePath = GetDatabasePath();
            LogToFile($"Путь к БД: {_databasePath}");
            InitializeDatabase();
        }

        public static string GetDatabasePath()
        {
            string fileName = "MedicineTrackerv2.db3";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);
            return path;
        }

        private void InitializeDatabase()
        {
            if (File.Exists(_databasePath))
            {
                LogToFile("БД уже существует.");
                return;
            }

            var assembly = typeof(DBConnection).Assembly;
            var resourceName = $"{assembly.GetName().Name}.MedicineTrackerv2.db3";

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                LogToFile($"Ресурс БД не найден: {resourceName}");
                throw new Exception("Ресурс БД не найден в сборке!");
            }

            try
            {
                using var fileStream = new FileStream(_databasePath, FileMode.Create);
                resourceStream.CopyTo(fileStream);
                LogToFile("БД успешно создана.");
            }
            catch (Exception ex)
            {
                LogToFile($"Ошибка создания БД: {ex.Message}");
                throw;
            }
        }

        private void LogToFile(string message)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_checks.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }


        private SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={_databasePath}");
        }

        private void AddParameters(SqliteCommand command, Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return;

            foreach (var param in parameters)
            {
                command.Parameters.Add(new SqliteParameter(param.Key, param.Value));
            }
        }

        public DataTable ExecuteReader(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();
                LogToFile($"Выполняется запрос: {query}");

                using var command = new SqliteCommand(query, connection);
                AddParameters(command, parameters);

                var dataTable = new DataTable();
                using var reader = command.ExecuteReader();
                dataTable.Load(reader);

                LogToFile($"Запрос выполнен успешно. Строк: {dataTable.Rows.Count}");
                return dataTable;
            }
            catch (SqliteException ex)
            {
                LogToFile($"Ошибка выполнения запроса: {query}\n{ex.Message}\n{ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                LogToFile($"Неизвестная ошибка: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        //public object? ExecuteScalar(string query, params SqliteParameter[] parameters)
        //{
        //    using var connection = CreateConnection();
        //    connection.Open();

        //    using var command = new SqliteCommand(query, connection);
        //    AddParameters(command, parameters);

        //    return command.ExecuteScalar();
        //}

        //public int ExecuteNonQuery(string query, params SqliteParameter[] parameters)
        //{
        //    using var connection = CreateConnection();
        //    connection.Open();

        //    using var command = new SqliteCommand(query, connection);
        //    AddParameters(command, parameters);

        //    return command.ExecuteNonQuery();
        //}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Здесь можно добавить очистку управляемых ресурсов
            }

            _disposed = true;
        }
    }
}
