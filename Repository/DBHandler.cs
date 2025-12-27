using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace MedicinesTracker.Repository
{
    public class DBHandler
    {
        private readonly string _connectionString;
        public DBHandler(string connectionString)
        {
            _connectionString = connectionString;
        }
        // для запросов select, возвращающих список
        
        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                return await connection.QueryAsync<T>(query, parameters);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"[DATABASE ERROR] {query} - {ex.Message}");
                throw;
            }
        }
        // для запросов select, возвращающих одно значение
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                return await connection.QueryFirstOrDefaultAsync<T>(query, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DATABASE ERROR] {query} - {ex.Message}");
                throw;
            }
        }
        // для запросов insert,update,delete
        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                return await connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DATABASE ERROR] {query} - {ex.Message}");
                throw;
            }
        }
        public async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand(sql, connection);

            // Добавляем параметры, если они переданы
            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    command.Parameters.AddWithValue(prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
                }
            }

            try
            {
                var result = await command.ExecuteScalarAsync();

                // Обрабатываем случай NULL из базы
                if (result == null || result == DBNull.Value)
                {
                    // Для nullable-типов возвращаем default
                    if (default(T) != null || Nullable.GetUnderlyingType(typeof(T)) != null)
                    {
                        return default!;
                    }

                    throw new InvalidOperationException("Cannot convert NULL to non-nullable type");
                }

                // Преобразуем результат к требуемому типу
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (SqliteException ex)
            {
                Debug.WriteLine($"[DATABASE ERROR] {sql} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DATABASE ERROR] {sql} - {ex.Message}");
                throw;
            }
        }
        // Просто добавьте этот метод в существующий класс DBHandler
        public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, Task<T>> operation)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var result = await operation(connection);
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Debug.WriteLine($"[TRANSACTION ERROR] {ex.Message}");
                throw;
            }
        }

    }
}
