using MedicinesTracker.Models;

namespace MedicinesTracker.Interface
{
    public interface IUnitRepository
    {
        Task<IEnumerable<UnitModel>> GetAllUnitsAsync();
    }
}
