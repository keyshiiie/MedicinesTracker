using MedicinesTracker.Models;

namespace MedicinesTracker.Repository
{
    public interface IStockRepository
    {
        Task<int> UpdateStockAsync(StockModel stockModel);
        Task<StockModel> GetStockByIdAsync(int idStock);
        Task<int> AddStockAsync(StockModel stockModel, int medicineId);
    }
    public class StockRepository : IStockRepository
    {
        private readonly DBHandler _dbHandler;
        public StockRepository(DBHandler bHandler)
        {
            _dbHandler = bHandler;
        }
        public async Task<int> UpdateStockAsync(StockModel stockModel)
        {
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
        public async Task<int> AddStockAsync(StockModel stockModel, int IdMedicine)
        {
            var query = @"
            INSERT INTO Stock (IdMedicine, Threshold, CurrentQuantity, ReminderEnabled) 
            VALUES (@IdMedicine, @Threshold, @CurrentQuantity, @ReminderEnabled)";
            var parameters = new
            {
                IdMedicine,
                stockModel.Threshold,
                stockModel.CurrentQuantity,
                stockModel.ReminderEnabled
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<StockModel> GetStockByIdAsync(int idStock)
        {
            var query = @"
            SELECT
                IdStock,
                CurrentQuantity,
                Threshold,
                ReminderEnabled
            FROM Stock
            WHERE IdStock = @IdStock";

            var parameters = new { IdStock = idStock };

            var stock = await _dbHandler.QueryFirstOrDefaultAsync<StockModel>(query, parameters);

            if (stock == null)
            {
                throw new KeyNotFoundException($"Запас с IdStock={idStock} не найден");
            }

            return stock;
        }
    }
}
