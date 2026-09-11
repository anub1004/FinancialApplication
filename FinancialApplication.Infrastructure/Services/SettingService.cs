using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Infrastructure.Services
{
    public class SettingService : ISettingService
    {
        private readonly AppDbContext _context;

        public SettingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SettingDto?> GetSettingsByUserIdAsync(Guid userId)
        {
            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (setting == null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return null;

                return new SettingDto
                {
                    Email = user.Email,
                    Name = user.Username,
                    Currency = "INR",
                    DefaultFy = "2025-26",
                    PreferredRegime = "New",
                    AvatarColor = "#6366f1"
                };
            }

            return new SettingDto
            {
                Email = setting.Email,
                Name = setting.Name,
                Currency = setting.Currency,
                DefaultFy = setting.DefaultFy,
                PreferredRegime = setting.PreferredRegime,
                AvatarColor = setting.AvatarColor
            };
        }

        public async Task<SettingDto> UpdateSettingAsync(Guid userId, SettingDto setting)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var existingSetting = await _context.Settings
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingSetting == null)
            {
                existingSetting = new Setting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Email = setting.Email ?? user.Email,
                    Name = string.IsNullOrWhiteSpace(setting.Name) ? user.Username : setting.Name,
                    Currency = string.IsNullOrWhiteSpace(setting.Currency) ? "INR" : setting.Currency,
                    DefaultFy = string.IsNullOrWhiteSpace(setting.DefaultFy) ? "2025-26" : setting.DefaultFy,
                    PreferredRegime = string.IsNullOrWhiteSpace(setting.PreferredRegime) ? "New" : setting.PreferredRegime,
                    AvatarColor = string.IsNullOrWhiteSpace(setting.AvatarColor) ? "#6366f1" : setting.AvatarColor,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Settings.AddAsync(existingSetting);
            }
            else
            {
                existingSetting.Name = setting.Name;
                existingSetting.Email = setting.Email ?? existingSetting.Email;
                existingSetting.Currency = setting.Currency;
                existingSetting.DefaultFy = setting.DefaultFy;
                existingSetting.PreferredRegime = setting.PreferredRegime;
                existingSetting.AvatarColor = setting.AvatarColor;
                existingSetting.UpdatedAt = DateTime.UtcNow;

                _context.Settings.Update(existingSetting);
            }

            await _context.SaveChangesAsync();

            return new SettingDto
            {
                Email = existingSetting.Email,
                Name = existingSetting.Name,
                Currency = existingSetting.Currency,
                DefaultFy = existingSetting.DefaultFy,
                PreferredRegime = existingSetting.PreferredRegime,
                AvatarColor = existingSetting.AvatarColor
            };
        }
    }
}
