using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class BudgetsController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly AppDbContext _context;

    public BudgetsController(IBudgetService budgetService, AppDbContext context)
    {
        _budgetService = budgetService;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;
        var userId = GetCurrentUserId();

        var budgets = await _budgetService.GetBudgetsAsync(userId, targetMonth, targetYear);

        // Fetch expenses in this month to calculate spent vs limit
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1);

        var monthExpenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.Type == Core.Enums.TransactionType.Expense && t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var overallSpent = monthExpenses.Sum(t => t.Amount);
        var categorySpent = monthExpenses
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        ViewBag.OverallSpent = overallSpent;
        ViewBag.CategorySpent = categorySpent;
        ViewBag.Month = targetMonth;
        ViewBag.Year = targetYear;
        ViewBag.MonthName = startDate.ToString("MMMM yyyy");

        ViewBag.Categories = await _context.Categories
            .Where(c => (c.UserId == null || c.UserId == userId) && c.Type == Core.Enums.TransactionType.Expense)
            .ToListAsync();

        return View(budgets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int? categoryId, decimal limitAmount, int? month, int? year)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;
        var userId = GetCurrentUserId();

        await _budgetService.SaveBudgetAsync(userId, categoryId, limitAmount, targetMonth, targetYear);
        TempData["Success"] = "Budget saved successfully!";
        return RedirectToAction(nameof(Index), new { month = targetMonth, year = targetYear });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        await _budgetService.DeleteBudgetAsync(userId, id);
        TempData["Success"] = "Budget removed";
        return RedirectToAction(nameof(Index));
    }
}
