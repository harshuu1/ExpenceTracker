using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Enums;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class RecurringController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly AppDbContext _context;

    public RecurringController(ITransactionService transactionService, AppDbContext context)
    {
        _transactionService = transactionService;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var recurring = await _context.RecurringTransactions
            .Include(r => r.Category)
            .Include(r => r.Account)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.DayOfMonth)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return View(recurring);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, decimal amount, int? categoryId, int? accountId, string paymentMethod, int dayOfMonth)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.Now;

        var nextDue = new DateTime(now.Year, now.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(now.Year, now.Month)));
        if (nextDue < now)
        {
            nextDue = nextDue.AddMonths(1);
        }

        var item = new RecurringTransaction
        {
            UserId = userId,
            Title = title,
            Amount = amount,
            Type = TransactionType.Expense,
            CategoryId = categoryId,
            AccountId = accountId,
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "UPI" : paymentMethod,
            Frequency = RecurrenceFrequency.Monthly,
            DayOfMonth = dayOfMonth,
            NextDueDate = nextDue,
            IsActive = true,
            AutoProcess = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecurringTransactions.Add(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Recurring expense '{title}' created!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessNow(int id)
    {
        var userId = GetCurrentUserId();
        var item = await _context.RecurringTransactions
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (item != null)
        {
            var dto = new TransactionCreateDto
            {
                Amount = item.Amount,
                Type = item.Type,
                CategoryId = item.CategoryId,
                AccountId = item.AccountId,
                PaymentMethod = item.PaymentMethod,
                Date = DateTime.Now,
                Note = $"Recurring: {item.Title}"
            };

            await _transactionService.AddTransactionAsync(userId, dto);
            item.LastProcessedDate = DateTime.UtcNow;
            item.NextDueDate = item.NextDueDate.AddMonths(1);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Processed ₹{item.Amount:N0} for {item.Title}!";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var item = await _context.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (item != null)
        {
            _context.RecurringTransactions.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Removed recurring expense";
        }
        return RedirectToAction(nameof(Index));
    }
}
