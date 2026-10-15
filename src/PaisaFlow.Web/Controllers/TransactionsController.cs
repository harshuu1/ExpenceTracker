using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IQuickAddParser _quickAddParser;
    private readonly AppDbContext _context;

    public TransactionsController(ITransactionService transactionService, IQuickAddParser quickAddParser, AppDbContext context)
    {
        _transactionService = transactionService;
        _quickAddParser = quickAddParser;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TransactionFilterDto filter)
    {
        var userId = GetCurrentUserId();
        var transactions = await _transactionService.GetFilteredTransactionsAsync(userId, filter);

        ViewBag.Filter = filter;
        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == null || c.UserId == userId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        ViewBag.Accounts = await _context.Accounts
            .Where(a => a.UserId == userId)
            .ToListAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_TransactionListPartial", transactions);
        }

        return View(transactions);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromForm] TransactionCreateDto dto)
    {
        if (dto.Amount <= 0)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please enter an amount greater than 0" });

            TempData["Error"] = "Amount must be greater than 0";
            return RedirectToAction(nameof(Index));
        }

        var userId = GetCurrentUserId();
        var tx = await _transactionService.AddTransactionAsync(userId, dto);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                message = $"₹{tx.Amount:N0} saved successfully!",
                id = tx.Id
            });
        }

        TempData["Success"] = $"Transaction of ₹{tx.Amount:N0} recorded!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] TransactionCreateDto dto)
    {
        var userId = GetCurrentUserId();
        var updated = await _transactionService.UpdateTransactionAsync(userId, id, dto);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = updated, message = updated ? "Updated successfully" : "Transaction not found" });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        var deleted = await _transactionService.DeleteTransactionAsync(userId, id);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = deleted, message = deleted ? "Transaction deleted" : "Not found" });
        }

        TempData["Success"] = "Transaction removed";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ParsePreview(string text)
    {
        var userId = GetCurrentUserId();
        var result = await _quickAddParser.ParseInputAsync(userId, text);
        return Json(result);
    }
}
