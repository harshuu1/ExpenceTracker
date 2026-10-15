using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Enums;
using PaisaFlow.Infrastructure.Data;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        ViewBag.Accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        ViewBag.Categories = await _context.Categories.Where(c => c.UserId == null || c.UserId == userId).OrderBy(c => c.DisplayOrder).ToListAsync();

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string currency, decimal monthlyBudgetLimit, bool isBiometricEnabled, string? pinCode)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FullName = fullName;
            user.Currency = currency;
            user.MonthlyBudgetLimit = monthlyBudgetLimit;
            user.IsBiometricEnabled = isBiometricEnabled;
            if (!string.IsNullOrWhiteSpace(pinCode)) user.PinCode = pinCode;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings updated successfully!";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string icon, string color, TransactionType type, decimal? defaultBudget)
    {
        var userId = GetCurrentUserId();
        var cat = new Category
        {
            UserId = userId,
            Name = name,
            Icon = string.IsNullOrWhiteSpace(icon) ? "📦" : icon,
            Color = string.IsNullOrWhiteSpace(color) ? "#6366F1" : color,
            Type = type,
            DefaultBudget = defaultBudget,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Category '{name}' created!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(string name, AccountType type, decimal initialBalance, string icon, string color, string? suffix)
    {
        var userId = GetCurrentUserId();
        var acc = new Account
        {
            UserId = userId,
            Name = name,
            Type = type,
            Balance = initialBalance,
            Icon = string.IsNullOrWhiteSpace(icon) ? "🏦" : icon,
            Color = string.IsNullOrWhiteSpace(color) ? "#3B82F6" : color,
            AccountNumberSuffix = suffix,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(acc);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Account '{name}' added!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTransactions()
    {
        var userId = GetCurrentUserId();

        // Remove transactions for this user
        var txs = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();
        _context.Transactions.RemoveRange(txs);

        // Reset account balances to 0
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        foreach (var acc in accounts)
        {
            acc.Balance = 0m;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Cleared {txs.Count} transactions and reset account balances!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FactoryReset()
    {
        var userId = GetCurrentUserId();

        // Remove user's transactions
        var txs = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();
        _context.Transactions.RemoveRange(txs);

        // Remove user's budgets
        var budgets = await _context.Budgets.Where(b => b.UserId == userId).ToListAsync();
        _context.Budgets.RemoveRange(budgets);

        // Remove user's recurring
        var recurring = await _context.RecurringTransactions.Where(r => r.UserId == userId).ToListAsync();
        _context.RecurringTransactions.RemoveRange(recurring);

        // Remove user's split groups
        var groups = await _context.SplitGroups
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Participants)
            .Include(g => g.Members)
            .Where(g => g.UserId == userId)
            .ToListAsync();
        _context.SplitGroups.RemoveRange(groups);

        // Remove user's custom accounts
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        _context.Accounts.RemoveRange(accounts);

        // Remove user's custom categories
        var customCats = await _context.Categories.Where(c => c.UserId == userId).ToListAsync();
        _context.Categories.RemoveRange(customCats);

        await _context.SaveChangesAsync();

        // Re-initialize clean fresh starter accounts & budget
        await DbInitializer.InitializeNewUserAsync(_context, userId);

        TempData["Success"] = "All data cleared! Your account is completely fresh.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDemoData()
    {
        var userId = GetCurrentUserId();

        // Clear existing data for user
        var txs = await _context.Transactions.Where(t => t.UserId == userId).ToListAsync();
        _context.Transactions.RemoveRange(txs);

        var budgets = await _context.Budgets.Where(b => b.UserId == userId).ToListAsync();
        _context.Budgets.RemoveRange(budgets);

        var recurring = await _context.RecurringTransactions.Where(r => r.UserId == userId).ToListAsync();
        _context.RecurringTransactions.RemoveRange(recurring);

        var groups = await _context.SplitGroups
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Participants)
            .Include(g => g.Members)
            .Where(g => g.UserId == userId)
            .ToListAsync();
        _context.SplitGroups.RemoveRange(groups);

        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        _context.Accounts.RemoveRange(accounts);

        await _context.SaveChangesAsync();

        // Re-seed demo data for Harsh/active user
        var now = DateTime.Now;
        var categories = await _context.Categories.ToListAsync();
        var foodCat = categories.FirstOrDefault(c => c.Name == "Food");
        var transCat = categories.FirstOrDefault(c => c.Name == "Transport");
        var shopCat = categories.FirstOrDefault(c => c.Name == "Shopping");
        var entCat = categories.FirstOrDefault(c => c.Name == "Entertainment");
        var grocCat = categories.FirstOrDefault(c => c.Name == "Groceries");
        var billsCat = categories.FirstOrDefault(c => c.Name == "Bills");
        var rechargeCat = categories.FirstOrDefault(c => c.Name == "Recharge");
        var travelCat = categories.FirstOrDefault(c => c.Name == "Travel");
        var salaryCat = categories.FirstOrDefault(c => c.Name == "Salary");
        var rentCat = categories.FirstOrDefault(c => c.Name == "Rent");

        var accSbi = new Account { UserId = userId, Name = "SBI Bank", Type = AccountType.Bank, Balance = 21400m, Icon = "🏦", Color = "#2563EB", IsDefault = true, AccountNumberSuffix = "4921" };
        var accHdfc = new Account { UserId = userId, Name = "HDFC Bank", Type = AccountType.Bank, Balance = 12800m, Icon = "🏦", Color = "#1E40AF", AccountNumberSuffix = "1088" };
        var accCash = new Account { UserId = userId, Name = "Cash", Type = AccountType.Cash, Balance = 3200m, Icon = "💵", Color = "#16A34A" };
        var accCc = new Account { UserId = userId, Name = "Credit Card", Type = AccountType.CreditCard, Balance = 8400m, Icon = "💳", Color = "#DC2626", AccountNumberSuffix = "8832" };
        _context.Accounts.AddRange(new[] { accSbi, accHdfc, accCash, accCc });
        await _context.SaveChangesAsync();

        // Budgets
        _context.Budgets.AddRange(new List<Budget>
        {
            new() { UserId = userId, CategoryId = null, LimitAmount = 30000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userId, CategoryId = foodCat?.Id, LimitAmount = 6000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userId, CategoryId = transCat?.Id, LimitAmount = 3000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userId, CategoryId = shopCat?.Id, LimitAmount = 4000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userId, CategoryId = entCat?.Id, LimitAmount = 2000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 }
        });

        // Recurring
        _context.RecurringTransactions.AddRange(new List<RecurringTransaction>
        {
            new() { UserId = userId, Title = "House Rent", Amount = 10000m, CategoryId = rentCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 1, NextDueDate = new DateTime(now.Year, now.Month, 1).AddMonths(1), IsActive = true, AutoProcess = true },
            new() { UserId = userId, Title = "Jio Fiber / Internet", Amount = 799m, CategoryId = billsCat?.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 15, NextDueDate = new DateTime(now.Year, now.Month, 15).AddMonths(1), IsActive = true, AutoProcess = true },
            new() { UserId = userId, Title = "Car EMI", Amount = 4500m, CategoryId = categories.FirstOrDefault(c => c.Name == "EMI")?.Id, AccountId = accSbi.Id, PaymentMethod = "Bank Transfer", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 7, NextDueDate = new DateTime(now.Year, now.Month, 7).AddMonths(1), IsActive = true, AutoProcess = true }
        });

        // Transactions matching prompt
        _context.Transactions.AddRange(new List<Transaction>
        {
            new() { UserId = userId, Amount = 42000m, Type = TransactionType.Income, CategoryId = salaryCat?.Id, AccountId = accSbi.Id, PaymentMethod = "Bank Transfer", Date = new DateTime(now.Year, now.Month, 1, 9, 30, 0), Note = "Monthly Salary Credited" },
            new() { UserId = userId, Amount = 10000m, Type = TransactionType.Expense, CategoryId = rentCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 1, 10, 0, 0), Note = "House Rent" },
            new() { UserId = userId, Amount = 1740m, Type = TransactionType.Expense, CategoryId = travelCat?.Id, AccountId = accCc.Id, PaymentMethod = "Credit Card", Date = new DateTime(now.Year, now.Month, 5, 14, 20, 0), Note = "Goa Train Tickets" },
            new() { UserId = userId, Amount = 240m, Type = TransactionType.Expense, CategoryId = transCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 8, 8, 45, 0), Note = "Uber to Office" },
            new() { UserId = userId, Amount = 120m, Type = TransactionType.Expense, CategoryId = transCat?.Id, AccountId = accCash.Id, PaymentMethod = "Cash", Date = new DateTime(now.Year, now.Month, 10, 19, 10, 0), Note = "Auto to Metro" },
            new() { UserId = userId, Amount = 299m, Type = TransactionType.Expense, CategoryId = rechargeCat?.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 12, 11, 15, 0), Note = "Jio Mobile Recharge" },
            new() { UserId = userId, Amount = 1250m, Type = TransactionType.Expense, CategoryId = grocCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 14, 18, 30, 0), Note = "Supermarket Grocery" },
            new() { UserId = userId, Amount = 491m, Type = TransactionType.Expense, CategoryId = billsCat?.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 14, 21, 0, 0), Note = "Electricity Bill" },
            new() { UserId = userId, Amount = 4100m, Type = TransactionType.Expense, CategoryId = foodCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 15, 13, 0, 0), Note = "Team Dinner & Cafes" },
            new() { UserId = userId, Amount = 180m, Type = TransactionType.Expense, CategoryId = foodCat?.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 15, 19, 32, 0), Note = "Lunch at Bistro" }
        });

        // Goa Split Group
        var splitGroup = new SplitGroup { UserId = userId, Name = "Trip to Goa", Description = "Weekend getaway with college buddies", Currency = "₹", CreatedAt = DateTime.UtcNow };
        _context.SplitGroups.Add(splitGroup);
        await _context.SaveChangesAsync();

        var mHarsh = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Harsh", UpiId = "harsh@okaxis", IsCurrentUser = true };
        var mAmit = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Amit", UpiId = "amit@okhdfc", IsCurrentUser = false };
        var mRahul = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Rahul", UpiId = "rahul@oksbi", IsCurrentUser = false };
        var mAkash = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Akash", UpiId = "akash@paytm", IsCurrentUser = false };
        var splitMembers = new List<SplitMember> { mHarsh, mAmit, mRahul, mAkash };
        _context.SplitMembers.AddRange(splitMembers);
        await _context.SaveChangesAsync();

        var exp1 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Beach Shack & Seafood Dinner", Amount = 7200m, PaidByMemberId = mHarsh.Id, Date = now.AddDays(-2) };
        var exp2 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Villa Stay Booking", Amount = 5400m, PaidByMemberId = mAmit.Id, Date = now.AddDays(-3) };
        var exp3 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Scooty & Fuel Rentals", Amount = 3000m, PaidByMemberId = mRahul.Id, Date = now.AddDays(-2) };
        var exp4 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Watersports at Baga", Amount = 3000m, PaidByMemberId = mAkash.Id, Date = now.AddDays(-1) };
        _context.SplitExpenses.AddRange(new[] { exp1, exp2, exp3, exp4 });
        await _context.SaveChangesAsync();

        foreach (var exp in new[] { exp1, exp2, exp3, exp4 })
        {
            var share = exp.Amount / 4m;
            foreach (var mem in splitMembers)
            {
                _context.SplitParticipants.Add(new SplitParticipant { SplitExpenseId = exp.Id, SplitMemberId = mem.Id, ShareAmount = share });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Restored October 2026 Demo Data (₹18,420 spending, Goa Trip, Budgets)!";
        return RedirectToAction(nameof(Index));
    }
}
