using MedicinesTracker.Models;

namespace MedicinesTracker.Repository
{
    public interface IRecipientRepository
    {
        Task<IEnumerable<RecipientModel>> GetAllRecipientsAsync();
        Task<int> UpdateRecipientAsync(RecipientModel recipientModel);
        Task<int> DeleteRecipientAsync(int idRecipient);
        Task<int> AddRecipientAsync(RecipientModel recipientModel);
    }
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

        public async Task<int> UpdateRecipientAsync(RecipientModel recipientModel)
        {
            var query = @"
            UPDATE Recipient
            SET 
                Name = @Name
            WHERE IdRecipient = @IdRecipient";
            var parameters = new
            {
                recipientModel.IdRecipient,
                recipientModel.Name
            };

            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<int> DeleteRecipientAsync(int idRecipient)
        {
            var query = @"DELETE FROM Recipient WHERE IdRecipient = @IdRecipient";
            var parameters = new
            {
                IdRecipient = idRecipient
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
        public async Task<int> AddRecipientAsync(RecipientModel recipientModel)
        {
            var query = @"INSERT INTO Recipient(Name) VALUES(@Name)";
            var parameters = new
            {
                recipientModel.Name
            };
            return await _dbHandler.ExecuteAsync(query, parameters);
        }
    }
}
