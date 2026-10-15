using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Enums;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction> AddTransactionAsync(int userId, TransactionCreateDto dto)
    {
        var tx = new Transaction
        {
            UserId = userId,
            Amount = dto.Amount,
            Type = dto.Type,
            CategoryId = dto.CategoryId,
            AccountId = dto.AccountId,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod,
            Date = dto.Date,
            Note = dto.Note,
            MerchantOrPayee = dto.MerchantOrPayee,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(tx);

        // Update Account Balance if account specified
        if (dto.AccountId.HasValue)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.AccountId.Value && a.UserId == userId);
            if (account != null)
            {
                if (dto.Type == TransactionType.Income)
                {
                    account.Balance += dto.Amount;
                }
                else if (dto.Type == TransactionType.Expense)
                {
                    account.Balance -= dto.Amount;
                }
            }
        }

        await _context.SaveChangesAsync();
        return tx;
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int userId, int transactionId)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
    }

    public async Task<bool> UpdateTransactionAsync(int userId, int transactionId, TransactionCreateDto dto)
    {
        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
        if (tx == null) return false;

        // Revert old account balance
        if (tx.AccountId.HasValue)
        {
            var oldAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == tx.AccountId.Value && a.UserId == userId);
            if (oldAccount != null)
            {
                if (tx.Type == TransactionType.Income) oldAccount.Balance -= tx.Amount;
                else if (tx.Type == TransactionType.Expense) oldAccount.Balance += tx.Amount;
            }
        }

        // Apply new values
        tx.Amount = dto.Amount;
        tx.Type = dto.Type;
        tx.CategoryId = dto.CategoryId;
        tx.AccountId = dto.AccountId;
        tx.PaymentMethod = dto.PaymentMethod;
        tx.Date = dto.Date;
        tx.Note = dto.Note;
        tx.MerchantOrPayee = dto.MerchantOrPayee;
        tx.UpdatedAt = DateTime.UtcNow;

        // Apply new account balance
        if (dto.AccountId.HasValue)
        {
            var newAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == dto.AccountId.Value && a.UserId == userId);
            if (newAccount != null)
            {
                if (dto.Type == TransactionType.Income) newAccount.Balance += dto.Amount;
                else if (dto.Type == TransactionType.Expense) newAccount.Balance -= dto.Amount;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTransactionAsync(int userId, int transactionId)
    {
        var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
        if (tx == null) return false;

        if (tx.AccountId.HasValue)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == tx.AccountId.Value && a.UserId == userId);
            if (account != null)
            {
                if (tx.Type == TransactionType.Income) account.Balance -= tx.Amount;
                else if (tx.Type == TransactionType.Expense) account.Balance += tx.Amount;
            }
        }

        _context.Transactions.Remove(tx);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Transaction>> GetFilteredTransactionsAsync(int userId, TransactionFilterDto filter)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var q = filter.Query.Trim().ToLower();
            query = query.Where(t => 
                (t.Note != null && t.Note.ToLower().Contains(q)) ||
                (t.MerchantOrPayee != null && t.MerchantOrPayee.ToLower().Contains(q)) ||
                (t.PaymentMethod != null && t.PaymentMethod.ToLower().Contains(q)) ||
                (t.Category != null && t.Category.Name.ToLower().Contains(q)) ||
                t.Amount.ToString().Contains(q));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

        if (filter.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filter.AccountId.Value);

        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod))
            query = query.Where(t => t.PaymentMethod == filter.PaymentMethod);

        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(t => t.Date >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(t => t.Date <= filter.EndDate.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= filter.MaxAmount.Value);

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int? month = null, int? year = null)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var userName = user?.FullName ?? "Harsh";
        var hideBalance = user?.HideBalance ?? false;

        // Greeting based on current local hour
        var hour = now.Hour;
        var greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        var monthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");

        // Accounts & Balances
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        var totalBalance = accounts.Sum(a => a.Balance);

        // Transactions in this month
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1);

        var monthTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var currentMonthIncome = monthTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var currentMonthExpenses = monthTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var netSavings = currentMonthIncome - currentMonthExpenses;

        // Monthly Budget
        var overallBudget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == null && b.Month == targetMonth && b.Year == targetYear);

        var budgetLimit = overallBudget?.LimitAmount ?? user?.MonthlyBudgetLimit ?? 42000m;
        var budgetRemaining = Math.Max(0, budgetLimit - currentMonthExpenses);
        var budgetUsedPercent = budgetLimit > 0 ? (int)Math.Min(100, Math.Round((currentMonthExpenses / budgetLimit) * 100)) : 0;
        var isBudgetWarning = budgetUsedPercent >= (overallBudget?.AlertThresholdPercent ?? 85);

        // Top categories
        var topCategories = monthTransactions
            .Where(t => t.Type == TransactionType.Expense && t.Category != null)
            .GroupBy(t => t.Category!)
            .Select(g => new CategorySpendSummaryDto
            {
                CategoryId = g.Key.Id,
                Name = g.Key.Name,
                Icon = g.Key.Icon,
                Color = g.Key.Color,
                TotalAmount = g.Sum(x => x.Amount),
                BudgetAmount = g.Key.DefaultBudget,
                PercentageOfTotal = currentMonthExpenses > 0 ? (int)Math.Round((g.Sum(x => x.Amount) / currentMonthExpenses) * 100) : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .Take(6)
            .ToList();

        // Recent 10 Transactions
        var recentTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(10)
            .ToListAsync();

        return new DashboardSummaryDto
        {
            UserName = userName,
            Greeting = greeting,
            MonthName = monthName,
            TotalBalance = totalBalance,
            CurrentMonthIncome = currentMonthIncome,
            CurrentMonthExpenses = currentMonthExpenses,
            NetSavings = netSavings,
            MonthlyBudget = budgetLimit,
            BudgetRemaining = budgetRemaining,
            BudgetUsedPercent = budgetUsedPercent,
            IsBudgetWarning = isBudgetWarning,
            HideBalance = hideBalance,
            TopCategories = topCategories,
            RecentTransactions = recentTransactions,
            Accounts = accounts
        };
    }
}
