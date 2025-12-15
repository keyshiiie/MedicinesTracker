using MedicinesTracker.Models;

namespace MedicinesTracker.Interface
{
    public interface IStockRepository
    {
        Task<int> UpdateStockAsync(StockModel stockModel);
    }
}
