using Dapper;
using MedicinesTracker.Interface;
using MedicinesTracker.Models;
using Microsoft.Data.Sqlite;

namespace MedicinesTracker.Repository
{
    public class UnitRepository : IUnitRepository
    {
        private readonly DBHandler _dbHandler;
        public UnitRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<IEnumerable<UnitModel>> GetAllUnitsAsync()
        {
            var query = @"SELECT * FROM Unit";
            return await _dbHandler.QueryAsync<UnitModel>(query);  

        }
    }
}
