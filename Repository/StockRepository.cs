using MedicinesTracker.Interface;
using MedicinesTracker.Models;

namespace MedicinesTracker.Repository
{
    public class StockRepository : IStockRepository
    {
        private readonly DBHandler _dbHandler;
        public StockRepository(DBHandler bHandler)
        {
            _dbHandler = bHandler;
        }
        public async Task<int> UpdateStockAsync(StockModel stockModel)
        {
            if (!stockModel.IdStock.HasValue)
                throw new ArgumentException("Для обновления требуется IdStock");
            var query = @"UPDATE Stock
            SET
            Threshold = @Threshold,
            CurrentQuantity  = @CurrentQuantity,
            ReminderEnabled = @ReminderEnabled
            WHERE IdStock = @IdStock";
            var parameters = new
            {
                stockModel.IdStock,
                stockModel.Threshold,
                stockModel.CurrentQuantity,
                stockModel.ReminderEnabled
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}
