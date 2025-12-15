using Dapper;
using MedicinesTracker.Interface;
using MedicinesTracker.Models;

namespace MedicinesTracker.Repository
{
    public class RecipientRepository : IRecipientRepository
    {
        private readonly DBHandler _dbHandler;
        public RecipientRepository(DBHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }
        public async Task<IEnumerable<RecipientModel>> GetAllRecipientsAsync()
        {
            var query = @"SELECT * FROM Recipient";
            return await _dbHandler.QueryAsync<RecipientModel>(query);
        }
    }
}
