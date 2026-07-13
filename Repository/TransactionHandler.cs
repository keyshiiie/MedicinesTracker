using MedicinesTracker.Data;

namespace MedicinesTracker.Repository
{
    public interface ITransactionHandler
    {
        Task ExecuteInTransactionAsync(Func<Task> operation);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }

    public class TransactionHandler : ITransactionHandler
    {
        private readonly AppDbContext _context;

        public TransactionHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
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