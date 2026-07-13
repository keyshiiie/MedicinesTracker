using MedicinesTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicinesTracker.Services
{
    public interface IAppSettingsService
    {
        Task<bool> IsFirstLaunchAsync();
        Task MarkFirstLaunchCompletedAsync();
    }

    public class AppSettingsService : IAppSettingsService
    {
        private readonly AppDbContext _context;

        public AppSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsFirstLaunchAsync()
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "FirstLaunchCompleted");

            return setting == null || setting.Value != "true";
        }

        public async Task MarkFirstLaunchCompletedAsync()
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.Key == "FirstLaunchCompleted");

            if (setting != null)
            {
                setting.Value = "true";
                await _context.SaveChangesAsync();
            }
        }
    }
}
