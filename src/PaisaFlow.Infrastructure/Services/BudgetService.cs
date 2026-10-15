using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _context;

    public BudgetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Budget>> GetBudgetsAsync(int userId, int? month = null, int? year = null)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        return await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Month == targetMonth && b.Year == targetYear)
            .ToListAsync();
    }

    public async Task<Budget> SaveBudgetAsync(int userId, int? categoryId, decimal limitAmount, int month, int year)
    {
        var existing = await _context.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.Month == month && b.Year == year);

        if (existing != null)
        {
            existing.LimitAmount = limitAmount;
            await _context.SaveChangesAsync();
            return existing;
        }

        var newBudget = new Budget
        {
            UserId = userId,
            CategoryId = categoryId,
            LimitAmount = limitAmount,
            Month = month,
            Year = year,
            AlertThresholdPercent = 85,
            CreatedAt = DateTime.UtcNow
        };

        _context.Budgets.Add(newBudget);
        await _context.SaveChangesAsync();
        return newBudget;
    }

    public async Task<bool> DeleteBudgetAsync(int userId, int budgetId)
    {
        var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);
        if (budget == null) return false;

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();
        return true;
    }
}
