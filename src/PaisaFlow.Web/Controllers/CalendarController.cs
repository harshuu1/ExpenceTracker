using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly AppDbContext _context;

    public CalendarController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year, int? selectedDay)
    {
        var now = DateTime.Now;
        var targetMonth = month ?? now.Month;
        var targetYear = year ?? now.Year;
        var targetDay = selectedDay ?? (targetMonth == now.Month && targetYear == now.Year ? now.Day : 15);

        var userId = GetCurrentUserId();

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1);

        var monthTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId && t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        // Daily totals map
        var dayTotals = monthTransactions
            .Where(t => t.Type == Core.Enums.TransactionType.Expense)
            .GroupBy(t => t.Date.Day)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        // Selected day transactions
        var selectedDayTransactions = monthTransactions
            .Where(t => t.Date.Day == targetDay)
            .OrderByDescending(t => t.Date)
            .ToList();

        var selectedDayTotal = selectedDayTransactions
            .Where(t => t.Type == Core.Enums.TransactionType.Expense)
            .Sum(t => t.Amount);

        ViewBag.Month = targetMonth;
        ViewBag.Year = targetYear;
        ViewBag.MonthName = startDate.ToString("MMMM yyyy");
        ViewBag.DaysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        ViewBag.FirstDayOfWeek = (int)startDate.DayOfWeek; // 0 = Sunday
        ViewBag.DayTotals = dayTotals;
        ViewBag.SelectedDay = targetDay;
        ViewBag.SelectedDayTotal = selectedDayTotal;
        ViewBag.SelectedDayDate = new DateTime(targetYear, targetMonth, Math.Min(targetDay, DateTime.DaysInMonth(targetYear, targetMonth)));

        return View(selectedDayTransactions);
    }
}
