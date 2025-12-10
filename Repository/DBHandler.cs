using Dapper;
using Microsoft.Data.Sqlite;
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
    }
}
