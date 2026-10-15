using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IQuickAddParser _quickAddParser;
    private readonly AppDbContext _context;

    public DashboardController(ITransactionService transactionService, IQuickAddParser quickAddParser, AppDbContext context)
    {
        _transactionService = transactionService;
        _quickAddParser = quickAddParser;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var userId = GetCurrentUserId();
        var summary = await _transactionService.GetDashboardSummaryAsync(userId, month, year);

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return View(summary);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> QuickAdd([FromForm] string input)
    {
        var userId = GetCurrentUserId();
        var parseResult = await _quickAddParser.ParseInputAsync(userId, input);

        if (!parseResult.Success)
        {
            return Json(new { success = false, message = parseResult.Message });
        }

        var dto = new TransactionCreateDto
        {
            Amount = parseResult.Amount,
            Type = parseResult.Type,
            CategoryId = parseResult.CategoryId,
            AccountId = parseResult.AccountId,
            PaymentMethod = parseResult.PaymentMethod,
            Date = parseResult.Date,
            Note = parseResult.Note
        };

        var tx = await _transactionService.AddTransactionAsync(userId, dto);

        return Json(new
        {
            success = true,
            message = $"Saved ₹{tx.Amount:N0} in {parseResult.CategoryName} via {tx.PaymentMethod}!",
            transaction = new
            {
                id = tx.Id,
                amount = tx.Amount,
                type = tx.Type.ToString(),
                category = parseResult.CategoryName,
                icon = parseResult.CategoryIcon,
                paymentMethod = tx.PaymentMethod,
                note = tx.Note,
                date = $"Today · {tx.Date:h:mm tt}"
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleHideBalance()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.HideBalance = !user.HideBalance;
            await _context.SaveChangesAsync();
            return Json(new { success = true, hideBalance = user.HideBalance });
        }
        return Json(new { success = false });
    }

    [HttpGet]
    public async Task<IActionResult> GetLiveStats()
    {
        var userId = GetCurrentUserId();
        var summary = await _transactionService.GetDashboardSummaryAsync(userId);
        return Json(summary);
    }
}
