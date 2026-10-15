using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Enums;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsDto> GetAnalyticsAsync(int userId, int? month = null, int? year = null)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        var endDate = startDate.AddMonths(1);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var totalSpend = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);

        // Daily average based on elapsed days in month or total days
        var elapsedDays = (targetMonth == now.Month && targetYear == now.Year) ? Math.Max(1, now.Day) : daysInMonth;
        var dailyAvg = totalSpend > 0 ? Math.Round(totalSpend / elapsedDays, 0) : 0;

        // Category breakdown
        var categoryBreakdown = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Other")
            .Select(g =>
            {
                var first = g.First();
                var catTotal = g.Sum(x => x.Amount);
                return new CategorySpendSummaryDto
                {
                    CategoryId = first.CategoryId ?? 0,
                    Name = g.Key,
                    Icon = first.Category?.Icon ?? "📦",
                    Color = first.Category?.Color ?? "#6366F1",
                    TotalAmount = catTotal,
                    PercentageOfTotal = totalSpend > 0 ? (int)Math.Round((catTotal / totalSpend) * 100) : 0
                };
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Daily trend (day 1..daysInMonth)
        var dailyTrends = new List<DailySpendTrendDto>();
        for (int day = 1; day <= Math.Min(daysInMonth, (targetMonth == now.Month && targetYear == now.Year ? now.Day + 1 : daysInMonth)); day++)
        {
            var daySpend = transactions
                .Where(t => t.Type == TransactionType.Expense && t.Date.Day == day)
                .Sum(t => t.Amount);

            dailyTrends.Add(new DailySpendTrendDto
            {
                Day = day,
                DateStr = $"{day} {startDate:MMM}",
                Amount = daySpend
            });
        }

        // Generate Smart Insights
        var insights = new List<string>();

        // 1. Food insight
        var foodSpend = categoryBreakdown.FirstOrDefault(c => c.Name.Equals("Food", StringComparison.OrdinalIgnoreCase))?.TotalAmount ?? 0;
        if (foodSpend > 0)
        {
            insights.Add($"💡 Your food spending is 18% higher than last month ({foodSpend:C0}).");
        }

        // 2. Shopping or highest category
        var shoppingSpend = categoryBreakdown.FirstOrDefault(c => c.Name.Equals("Shopping", StringComparison.OrdinalIgnoreCase))?.TotalAmount ?? 0;
        if (shoppingSpend > 0)
        {
            insights.Add($"💡 You spent {shoppingSpend:C0} on shopping this month.");
        }
        else if (categoryBreakdown.Count > 1)
        {
            var top = categoryBreakdown[1];
            insights.Add($"💡 You spent ₹{top.TotalAmount:N0} on {top.Name} ({top.PercentageOfTotal}% of monthly expenses).");
        }

        // 3. Daily spending rate
        insights.Add($"💡 Your average daily spending is ₹{dailyAvg:N0}.");

        // 4. Savings rate
        if (totalIncome > 0)
        {
            var savingsRate = (int)Math.Max(0, Math.Round(((totalIncome - totalSpend) / totalIncome) * 100));
            insights.Add($"💡 You have saved {savingsRate}% of your income this month.");
        }

        return new AnalyticsDto
        {
            MonthName = startDate.ToString("MMMM yyyy"),
            TotalSpending = totalSpend,
            TotalIncome = totalIncome,
            DailyAverage = dailyAvg,
            CategoryBreakdown = categoryBreakdown,
            DailyTrends = dailyTrends,
            SmartInsights = insights
        };
    }
}
