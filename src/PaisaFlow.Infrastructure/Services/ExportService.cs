using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _context;

    public ExportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportCsvAsync(int userId, int? month = null, int? year = null)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId);

        if (month.HasValue && year.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1);
            var end = start.AddMonths(1);
            query = query.Where(t => t.Date >= start && t.Date < end);
        }

        var list = await query.OrderByDescending(t => t.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Date,Type,Category,Amount,PaymentMethod,Account,Note,Merchant");

        foreach (var t in list)
        {
            var note = t.Note?.Replace("\"", "\"\"") ?? "";
            var merchant = t.MerchantOrPayee?.Replace("\"", "\"\"") ?? "";
            var category = t.Category?.Name ?? "Uncategorized";
            var account = t.Account?.Name ?? "";

            sb.AppendLine($"{t.Id},{t.Date:yyyy-MM-dd HH:mm},{t.Type},\"{category}\",{t.Amount:F2},\"{t.PaymentMethod}\",\"{account}\",\"{note}\",\"{merchant}\"");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportExcelTsvAsync(int userId, int? month = null, int? year = null)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.UserId == userId);

        if (month.HasValue && year.HasValue)
        {
            var start = new DateTime(year.Value, month.Value, 1);
            var end = start.AddMonths(1);
            query = query.Where(t => t.Date >= start && t.Date < end);
        }

        var list = await query.OrderByDescending(t => t.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID\tDate\tType\tCategory\tAmount (₹)\tPayment Method\tAccount\tNote\tPayee");

        foreach (var t in list)
        {
            var category = t.Category?.Name ?? "Uncategorized";
            var account = t.Account?.Name ?? "";
            sb.AppendLine($"{t.Id}\t{t.Date:dd/MM/yyyy HH:mm}\t{t.Type}\t{category}\t{t.Amount:F2}\t{t.PaymentMethod}\t{account}\t{t.Note}\t{t.MerchantOrPayee}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<string> ExportJsonBackupAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        var categories = await _context.Categories.Where(c => c.UserId == userId || c.IsSystem).ToListAsync();
        var transactions = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();
        var budgets = await _context.Budgets.Where(b => b.UserId == userId).ToListAsync();
        var recurring = await _context.RecurringTransactions.Where(r => r.UserId == userId).ToListAsync();

        var backup = new
        {
            ExportedAt = DateTime.UtcNow,
            App = "PaisaFlow",
            Version = "1.0",
            User = user,
            Accounts = accounts,
            Categories = categories,
            Transactions = transactions,
            Budgets = budgets,
            RecurringTransactions = recurring
        };

        return JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<bool> ImportJsonBackupAsync(int userId, string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            // Valid JSON check
            return true;
        }
        catch
        {
            return false;
        }
    }
}
