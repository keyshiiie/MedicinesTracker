using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IStockRepository
    {
        Task<int> UpdateStockAsync(Stock stock);
        Task<Stock?> GetStockByIdAsync(int idStock);
        Task<int> AddStockAsync(Stock stock);
        Task<int> UpdateCurrentQuantityAsync(int idStock, int quantity);
    }

    public class StockRepository : IStockRepository
    {
        private readonly AppDbContext _context;

        public StockRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> UpdateStockAsync(Stock stock)
        {
            var existing = await _context.Stocks.FindAsync(stock.IdStock);
            if (existing == null) return 0;

            existing.Threshold = stock.Threshold;
            existing.CurrentQuantity = stock.CurrentQuantity;
            existing.ReminderEnabled = stock.ReminderEnabled;

            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateCurrentQuantityAsync(int idStock, int quantity)
        {
            var stock = await _context.Stocks.FindAsync(idStock);
            if (stock == null) return 0;

            stock.CurrentQuantity = quantity;
            return await _context.SaveChangesAsync();
        }

        public async Task<int> AddStockAsync(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
            return stock.IdStock;
        }

        public async Task<Stock?> GetStockByIdAsync(int idStock)
        {
            var stock = await _context.Stocks.FindAsync(idStock);
            if (stock == null)
                throw new KeyNotFoundException($"Запас с IdStock={idStock} не найден");

            return stock;
        }
    }
}