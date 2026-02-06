using MedicinesTracker.Services;
using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace MedicinesTracker.Repository
{
    public interface ITransactionHandler
    {
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }

    public class TransactionHandler : ITransactionHandler
    {
        private readonly IDatabaseService _databaseService;

        public TransactionHandler(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            // Создаем новое соединение для каждой транзакции
            using var connection = await _databaseService.GetOpenConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Выполняем операцию
                var result = await operation();

                // Коммитим транзакцию СИНХРОННО
                transaction.Commit();
                return result;
            }
            catch
            {
                // Откатываем при ошибке СИНХРОННО
                transaction.Rollback();
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            await ExecuteInTransactionAsync<object?>(async () =>
            {
                await operation();
                return null;
            });
        }
    }
}