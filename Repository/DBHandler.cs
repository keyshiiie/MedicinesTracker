using Dapper;
using MedicinesTracker.Services;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace MedicinesTracker.Repository
{
    // Добавляем интерфейс для DI
    public interface IDBHandler
    {
        Task<IEnumerable<T>> QueryAsync<T>(string query, object? parameters = null);
        Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null);
        Task<int> ExecuteAsync(string query, object? parameters = null);
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);
    }

    public class DBHandler : IDBHandler
    {
        private readonly IDatabaseService _databaseService;

        public DBHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // Вспомогательный метод для выполнения операций с открытым соединением
        private async Task<T> ExecuteWithConnectionAsync<T>(Func<SqliteConnection, Task<T>> operation)
        {
            using var connection = await _databaseService.GetOpenConnectionAsync();
            return await operation(connection);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object? parameters = null)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                return await connection.QueryAsync<T>(query, parameters);
            });
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                return await connection.QueryFirstOrDefaultAsync<T>(query, parameters);
            });
        }

        public async Task<int> ExecuteAsync(string query, object? parameters = null)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                return await connection.ExecuteAsync(query, parameters);
            });
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                return await connection.ExecuteScalarAsync<T?>(sql, parameters);
            });
        }
    }
}