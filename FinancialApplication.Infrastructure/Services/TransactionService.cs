using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Transaction;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        // ── Predefined Categories ────────────────────────────────────────────
        private static readonly List<CategoryDto> PredefinedCategories = new()
        {
            // Expense categories
            new() { Name = "Food & Dining",    Type = "Expense", Icon = "🍕" },
            new() { Name = "Groceries",        Type = "Expense", Icon = "🛒" },
            new() { Name = "Transport",        Type = "Expense", Icon = "🚗" },
            new() { Name = "Rent",             Type = "Expense", Icon = "🏠" },
            new() { Name = "Bills & Utilities", Type = "Expense", Icon = "💡" },
            new() { Name = "Shopping",         Type = "Expense", Icon = "🛍️" },
            new() { Name = "Entertainment",    Type = "Expense", Icon = "🎬" },
            new() { Name = "Healthcare",       Type = "Expense", Icon = "🏥" },
            new() { Name = "Education",        Type = "Expense", Icon = "📚" },
            new() { Name = "Travel",           Type = "Expense", Icon = "✈️" },
            new() { Name = "Insurance",        Type = "Expense", Icon = "🛡️" },
            new() { Name = "Personal Care",    Type = "Expense", Icon = "💅" },
            new() { Name = "Gifts & Donations",Type = "Expense", Icon = "🎁" },
            new() { Name = "Subscriptions",    Type = "Expense", Icon = "📱" },
            new() { Name = "EMI / Loan",       Type = "Expense", Icon = "🏦" },

            // Income categories
            new() { Name = "Salary",           Type = "Income", Icon = "💰" },
            new() { Name = "Freelance",        Type = "Income", Icon = "💻" },
            new() { Name = "Business",         Type = "Income", Icon = "🏢" },
            new() { Name = "Interest",         Type = "Income", Icon = "🏦" },
            new() { Name = "Dividend",         Type = "Income", Icon = "📈" },
            new() { Name = "Rental Income",    Type = "Income", Icon = "🏠" },
            new() { Name = "Refund",           Type = "Income", Icon = "↩️" },
            new() { Name = "Gift Received",    Type = "Income", Icon = "🎁" },
            new() { Name = "Cashback",         Type = "Income", Icon = "💳" },

            // Both
            new() { Name = "Investment",       Type = "Both", Icon = "📊" },
            new() { Name = "Transfer",         Type = "Both", Icon = "🔄" },
            new() { Name = "Other",            Type = "Both", Icon = "📝" },
        };

        // ── Create ──────────────────────────────────────────────────────────
        public async Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto dto)
        {
            var transaction = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = userId,
                Amount = dto.Amount,
                Category = dto.Category,
                Description = dto.Description,
                TransactionDate = dto.TransactionDate,
                TransactionType = dto.TransactionType,
                Currency = dto.Currency,
                PaymentMethod = dto.PaymentMethod,
                IsRecurring = dto.IsRecurring,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return MapToDto(transaction);
        }

        // ── Get by ID ───────────────────────────────────────────────────────
        public async Task<TransactionDto?> GetByIdAsync(Guid userId, Guid transactionId)
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);

            return transaction == null ? null : MapToDto(transaction);
        }

        // ── Get All (with filters, search, sort, pagination) ────────────────
        public async Task<(List<TransactionDto> Items, int TotalCount)> GetAllAsync(
            Guid userId,
            string? category = null,
            string? type = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            string? search = null,
            string sortBy = "TransactionDate",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(t => t.Category == category);

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (Enum.TryParse<TransactionTypeEnum>(type, true, out var typeEnum))
                    query = query.Where(t => t.TransactionType == typeEnum);
            }

            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.TransactionDate <= toDate.Value);

            if (minAmount.HasValue)
                query = query.Where(t => t.Amount >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(t => t.Amount <= maxAmount.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(t =>
                    t.Description.ToLower().Contains(searchLower) ||
                    t.Category.ToLower().Contains(searchLower));
            }

            // Count before pagination
            var totalCount = await query.CountAsync();

            // Sort
            query = sortBy?.ToLower() switch
            {
                "amount" => sortOrder == "asc" ? query.OrderBy(t => t.Amount) : query.OrderByDescending(t => t.Amount),
                "category" => sortOrder == "asc" ? query.OrderBy(t => t.Category) : query.OrderByDescending(t => t.Category),
                "createdat" => sortOrder == "asc" ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt),
                _ => sortOrder == "asc" ? query.OrderBy(t => t.TransactionDate) : query.OrderByDescending(t => t.TransactionDate),
            };

            // Paginate
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.Select(MapToDto).ToList(), totalCount);
        }

        // ── Update ──────────────────────────────────────────────────────────
        public async Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionDto dto)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId)
                ?? throw new KeyNotFoundException("Transaction not found.");

            if (dto.Amount.HasValue) transaction.Amount = dto.Amount.Value;
            if (dto.Category != null) transaction.Category = dto.Category;
            if (dto.Description != null) transaction.Description = dto.Description;
            if (dto.TransactionDate.HasValue) transaction.TransactionDate = dto.TransactionDate.Value;
            if (dto.TransactionType.HasValue) transaction.TransactionType = dto.TransactionType.Value;
            if (dto.Currency != null) transaction.Currency = dto.Currency;
            if (dto.PaymentMethod != null) transaction.PaymentMethod = dto.PaymentMethod;
            if (dto.IsRecurring.HasValue) transaction.IsRecurring = dto.IsRecurring.Value;

            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(transaction);
        }

        // ── Delete ──────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid userId, Guid transactionId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);

            if (transaction == null) return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Monthly Summary ─────────────────────────────────────────────────
        public async Task<TransactionSummaryDto> GetMonthlySummaryAsync(Guid userId, int? month = null, int? year = null)
        {
            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && t.TransactionDate.Month == targetMonth
                    && t.TransactionDate.Year == targetYear)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.TransactionType == TransactionTypeEnum.Income)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.TransactionType == TransactionTypeEnum.Expense)
                .Sum(t => t.Amount);

            return new TransactionSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetBalance = totalIncome - totalExpense,
                TransactionCount = transactions.Count,
                Currency = "INR", // TODO: use user's preferred currency
                Month = targetMonth,
                Year = targetYear
            };
        }

        // ── Categories ──────────────────────────────────────────────────────
        public Task<List<CategoryDto>> GetCategoriesAsync()
        {
            // Return predefined categories (users can also type custom category names)
            return Task.FromResult(PredefinedCategories.ToList());
        }

        // ── Mapping ─────────────────────────────────────────────────────────
        private static TransactionDto MapToDto(Transaction t) => new()
        {
            TransactionId = t.TransactionId,
            Amount = t.Amount,
            Category = t.Category,
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            TransactionType = t.TransactionType,
            TransactionTypeName = t.TransactionType.ToString(),
            Currency = t.Currency,
            PaymentMethod = t.PaymentMethod,
            IsRecurring = t.IsRecurring,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
