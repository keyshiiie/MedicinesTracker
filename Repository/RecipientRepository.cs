using MedicinesTracker.Data;
using MedicinesTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Repository
{
    public interface IRecipientRepository
    {
        Task<IEnumerable<Recipient>> GetAllRecipientsAsync();
        Task<int> UpdateRecipientAsync(Recipient recipient);
        Task<int> DeleteRecipientAsync(int idRecipient);
        Task<int> AddRecipientAsync(Recipient recipient);
    }

    public class RecipientRepository : IRecipientRepository
    {
        private readonly AppDbContext _context;

        public RecipientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Recipient>> GetAllRecipientsAsync()
        {
            return await _context.Recipients
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<int> UpdateRecipientAsync(Recipient recipient)
        {
            var existing = await _context.Recipients.FindAsync(recipient.IdRecipient);
            if (existing == null) return 0;

            existing.Name = recipient.Name;
            existing.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            return await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteRecipientAsync(int idRecipient)
        {
            var recipient = await _context.Recipients
                .Include(r => r.Medicines)
                .FirstOrDefaultAsync(r => r.IdRecipient == idRecipient);

            if (recipient == null) return 0;

            _context.Recipients.Remove(recipient);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> AddRecipientAsync(Recipient recipient)
        {
            recipient.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _context.Recipients.Add(recipient);
            await _context.SaveChangesAsync();
            return recipient.IdRecipient;
        }
    }
}