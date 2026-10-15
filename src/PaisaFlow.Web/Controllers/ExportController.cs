using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly IExportService _exportService;
    private readonly AppDbContext _context;

    public ExportController(IExportService exportService, AppDbContext context)
    {
        _exportService = exportService;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Csv(int? month, int? year)
    {
        var userId = GetCurrentUserId();
        var bytes = await _exportService.ExportCsvAsync(userId, month, year);
        var filename = $"PaisaFlow_Transactions_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpGet]
    public async Task<IActionResult> Excel(int? month, int? year)
    {
        var userId = GetCurrentUserId();
        var bytes = await _exportService.ExportExcelTsvAsync(userId, month, year);
        var filename = $"PaisaFlow_Report_{DateTime.Now:yyyyMMdd_HHmm}.xls";
        return File(bytes, "application/vnd.ms-excel; charset=utf-8", filename);
    }

    [HttpGet]
    public async Task<IActionResult> Backup()
    {
        var userId = GetCurrentUserId();
        var json = await _exportService.ExportJsonBackupAsync(userId);
        var bytes = Encoding.UTF8.GetBytes(json);
        var filename = $"PaisaFlow_Backup_{DateTime.Now:yyyyMMdd}.json";
        return File(bytes, "application/json", filename);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(IFormFile? backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["Error"] = "Please select a valid backup JSON file";
            return RedirectToAction("Index", "Settings");
        }

        using var reader = new StreamReader(backupFile.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var userId = GetCurrentUserId();
        var ok = await _exportService.ImportJsonBackupAsync(userId, content);

        if (ok) TempData["Success"] = "Data verified and restored!";
        else TempData["Error"] = "Failed to restore backup. Invalid format.";

        return RedirectToAction("Index", "Settings");
    }
}
