using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Enums;

namespace PaisaFlow.Infrastructure.Data;

public static class DbInitializer
{
    public static string GenerateSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static string HashPassword(string password, string salt)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
        {
            return; // Already seeded
        }

        // 1. Seed User: Harsh
        var harshSalt = GenerateSalt();
        var userHarsh = new UserAccount
        {
            FullName = "Harsh",
            Email = "harsh@example.com",
            PasswordSalt = harshSalt,
            PasswordHash = HashPassword("Password@123", harshSalt),
            Currency = "₹",
            MonthlyBudgetLimit = 42000m,
            HideBalance = false,
            PinCode = "1234",
            IsBiometricEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(userHarsh);

        // 2. Seed Second User: Priya (to test real multi-user isolation!)
        var priyaSalt = GenerateSalt();
        var userPriya = new UserAccount
        {
            FullName = "Priya Sharma",
            Email = "priya@example.com",
            PasswordSalt = priyaSalt,
            PasswordHash = HashPassword("Password@123", priyaSalt),
            Currency = "₹",
            MonthlyBudgetLimit = 35000m,
            HideBalance = false,
            PinCode = "5678",
            IsBiometricEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(userPriya);
        await context.SaveChangesAsync();

        // 3. Seed System Categories
        var categories = GetDefaultCategories();
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // 4. Seed Accounts for Harsh
        var accSbi = new Account { UserId = userHarsh.Id, Name = "SBI Bank", Type = AccountType.Bank, Balance = 21400m, Icon = "🏦", Color = "#2563EB", IsDefault = true, AccountNumberSuffix = "4921" };
        var accHdfc = new Account { UserId = userHarsh.Id, Name = "HDFC Bank", Type = AccountType.Bank, Balance = 12800m, Icon = "🏦", Color = "#1E40AF", AccountNumberSuffix = "1088" };
        var accCash = new Account { UserId = userHarsh.Id, Name = "Cash", Type = AccountType.Cash, Balance = 3200m, Icon = "💵", Color = "#16A34A" };
        var accCc = new Account { UserId = userHarsh.Id, Name = "Credit Card", Type = AccountType.CreditCard, Balance = 8400m, Icon = "💳", Color = "#DC2626", AccountNumberSuffix = "8832" };
        context.Accounts.AddRange(new[] { accSbi, accHdfc, accCash, accCc });

        // 5. Seed Accounts for Priya
        var priyaIcici = new Account { UserId = userPriya.Id, Name = "ICICI Bank", Type = AccountType.Bank, Balance = 48500m, Icon = "🏦", Color = "#EA580C", IsDefault = true, AccountNumberSuffix = "7721" };
        var priyaCash = new Account { UserId = userPriya.Id, Name = "Cash", Type = AccountType.Cash, Balance = 5400m, Icon = "💵", Color = "#16A34A" };
        var priyaPaytm = new Account { UserId = userPriya.Id, Name = "Paytm Wallet", Type = AccountType.Wallet, Balance = 2150m, Icon = "📱", Color = "#0284C7" };
        context.Accounts.AddRange(new[] { priyaIcici, priyaCash, priyaPaytm });
        await context.SaveChangesAsync();

        // 6. Seed Budgets for Harsh
        var now = DateTime.Now;
        var foodCat = categories.First(c => c.Name == "Food");
        var transCat = categories.First(c => c.Name == "Transport");
        var shopCat = categories.First(c => c.Name == "Shopping");
        var entCat = categories.First(c => c.Name == "Entertainment");
        var grocCat = categories.First(c => c.Name == "Groceries");
        var billsCat = categories.First(c => c.Name == "Bills");
        var rechargeCat = categories.First(c => c.Name == "Recharge");
        var travelCat = categories.First(c => c.Name == "Travel");
        var salaryCat = categories.First(c => c.Name == "Salary");
        var rentCat = categories.First(c => c.Name == "Rent");

        context.Budgets.AddRange(new List<Budget>
        {
            new() { UserId = userHarsh.Id, CategoryId = null, LimitAmount = 30000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userHarsh.Id, CategoryId = foodCat.Id, LimitAmount = 6000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userHarsh.Id, CategoryId = transCat.Id, LimitAmount = 3000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userHarsh.Id, CategoryId = shopCat.Id, LimitAmount = 4000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 },
            new() { UserId = userHarsh.Id, CategoryId = entCat.Id, LimitAmount = 2000m, Month = now.Month, Year = now.Year, AlertThresholdPercent = 85 }
        });

        // 7. Seed Recurring Transactions for Harsh
        context.RecurringTransactions.AddRange(new List<RecurringTransaction>
        {
            new() { UserId = userHarsh.Id, Title = "House Rent", Amount = 10000m, CategoryId = rentCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 1, NextDueDate = new DateTime(now.Year, now.Month, 1).AddMonths(1), IsActive = true, AutoProcess = true },
            new() { UserId = userHarsh.Id, Title = "Jio Fiber / Internet", Amount = 799m, CategoryId = billsCat.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 15, NextDueDate = new DateTime(now.Year, now.Month, 15).AddMonths(1), IsActive = true, AutoProcess = true },
            new() { UserId = userHarsh.Id, Title = "Car EMI", Amount = 4500m, CategoryId = categories.First(c => c.Name == "EMI").Id, AccountId = accSbi.Id, PaymentMethod = "Bank Transfer", Frequency = RecurrenceFrequency.Monthly, DayOfMonth = 7, NextDueDate = new DateTime(now.Year, now.Month, 7).AddMonths(1), IsActive = true, AutoProcess = true }
        });

        // 8. Seed Realistic October Transactions for Harsh
        // Total Income: ₹42,000, Total Expenses: ₹18,420, Remaining: ₹23,580
        var txDate = new DateTime(now.Year, now.Month, Math.Min(15, DateTime.DaysInMonth(now.Year, now.Month)));
        var harshTransactions = new List<Transaction>
        {
            new() { UserId = userHarsh.Id, Amount = 42000m, Type = TransactionType.Income, CategoryId = salaryCat.Id, AccountId = accSbi.Id, PaymentMethod = "Bank Transfer", Date = new DateTime(now.Year, now.Month, 1, 9, 30, 0), Note = "Monthly Salary Credited" },
            new() { UserId = userHarsh.Id, Amount = 10000m, Type = TransactionType.Expense, CategoryId = rentCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 1, 10, 0, 0), Note = "House Rent" },
            new() { UserId = userHarsh.Id, Amount = 1740m, Type = TransactionType.Expense, CategoryId = travelCat.Id, AccountId = accCc.Id, PaymentMethod = "Credit Card", Date = new DateTime(now.Year, now.Month, 5, 14, 20, 0), Note = "Goa Train Tickets" },
            new() { UserId = userHarsh.Id, Amount = 240m, Type = TransactionType.Expense, CategoryId = transCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 8, 8, 45, 0), Note = "Uber to Office" },
            new() { UserId = userHarsh.Id, Amount = 120m, Type = TransactionType.Expense, CategoryId = transCat.Id, AccountId = accCash.Id, PaymentMethod = "Cash", Date = new DateTime(now.Year, now.Month, 10, 19, 10, 0), Note = "Auto to Metro" },
            new() { UserId = userHarsh.Id, Amount = 299m, Type = TransactionType.Expense, CategoryId = rechargeCat.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 12, 11, 15, 0), Note = "Jio Mobile Recharge" },
            new() { UserId = userHarsh.Id, Amount = 1250m, Type = TransactionType.Expense, CategoryId = grocCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 14, 18, 30, 0), Note = "Supermarket Grocery" },
            new() { UserId = userHarsh.Id, Amount = 491m, Type = TransactionType.Expense, CategoryId = billsCat.Id, AccountId = accHdfc.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 14, 21, 0, 0), Note = "Electricity Bill" },
            new() { UserId = userHarsh.Id, Amount = 4100m, Type = TransactionType.Expense, CategoryId = foodCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 15, 13, 0, 0), Note = "Team Dinner & Cafes" },
            new() { UserId = userHarsh.Id, Amount = 180m, Type = TransactionType.Expense, CategoryId = foodCat.Id, AccountId = accSbi.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 15, 19, 32, 0), Note = "Lunch at Bistro" }
        };
        context.Transactions.AddRange(harshTransactions);

        // 9. Seed Distinct Transactions for Priya (demonstrating user isolation)
        var priyaTransactions = new List<Transaction>
        {
            new() { UserId = userPriya.Id, Amount = 55000m, Type = TransactionType.Income, CategoryId = salaryCat.Id, AccountId = priyaIcici.Id, PaymentMethod = "Bank Transfer", Date = new DateTime(now.Year, now.Month, 1, 10, 0, 0), Note = "Software Engineer Salary" },
            new() { UserId = userPriya.Id, Amount = 12500m, Type = TransactionType.Expense, CategoryId = rentCat.Id, AccountId = priyaIcici.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 2, 11, 0, 0), Note = "Apartment Rent" },
            new() { UserId = userPriya.Id, Amount = 3400m, Type = TransactionType.Expense, CategoryId = shopCat.Id, AccountId = priyaIcici.Id, PaymentMethod = "Debit Card", Date = new DateTime(now.Year, now.Month, 6, 17, 30, 0), Note = "Zara Dress & Accessories" },
            new() { UserId = userPriya.Id, Amount = 980m, Type = TransactionType.Expense, CategoryId = foodCat.Id, AccountId = priyaPaytm.Id, PaymentMethod = "UPI", Date = new DateTime(now.Year, now.Month, 10, 20, 15, 0), Note = "Dinner with Friends" },
            new() { UserId = userPriya.Id, Amount = 450m, Type = TransactionType.Expense, CategoryId = transCat.Id, AccountId = priyaCash.Id, PaymentMethod = "Cash", Date = new DateTime(now.Year, now.Month, 14, 9, 45, 0), Note = "Airport Cab" }
        };
        context.Transactions.AddRange(priyaTransactions);

        // 10. Seed Goa Trip Split Group for Harsh
        var splitGroup = new SplitGroup
        {
            UserId = userHarsh.Id,
            Name = "Trip to Goa",
            Description = "Weekend getaway with college buddies",
            Currency = "₹",
            CreatedAt = DateTime.UtcNow
        };
        context.SplitGroups.Add(splitGroup);
        await context.SaveChangesAsync();

        var mHarsh = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Harsh", UpiId = "harsh@okaxis", IsCurrentUser = true };
        var mAmit = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Amit", UpiId = "amit@okhdfc", IsCurrentUser = false };
        var mRahul = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Rahul", UpiId = "rahul@oksbi", IsCurrentUser = false };
        var mAkash = new SplitMember { SplitGroupId = splitGroup.Id, Name = "Akash", UpiId = "akash@paytm", IsCurrentUser = false };
        var splitMembers = new List<SplitMember> { mHarsh, mAmit, mRahul, mAkash };
        context.SplitMembers.AddRange(splitMembers);
        await context.SaveChangesAsync();

        var exp1 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Beach Shack & Seafood Dinner", Amount = 7200m, PaidByMemberId = mHarsh.Id, Date = txDate.AddDays(-2) };
        var exp2 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Villa Stay Booking", Amount = 5400m, PaidByMemberId = mAmit.Id, Date = txDate.AddDays(-3) };
        var exp3 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Scooty & Fuel Rentals", Amount = 3000m, PaidByMemberId = mRahul.Id, Date = txDate.AddDays(-2) };
        var exp4 = new SplitExpense { SplitGroupId = splitGroup.Id, Title = "Watersports at Baga", Amount = 3000m, PaidByMemberId = mAkash.Id, Date = txDate.AddDays(-1) };
        context.SplitExpenses.AddRange(new[] { exp1, exp2, exp3, exp4 });
        await context.SaveChangesAsync();

        foreach (var exp in new[] { exp1, exp2, exp3, exp4 })
        {
            var share = exp.Amount / 4m;
            foreach (var mem in splitMembers)
            {
                context.SplitParticipants.Add(new SplitParticipant
                {
                    SplitExpenseId = exp.Id,
                    SplitMemberId = mem.Id,
                    ShareAmount = share
                });
            }
        }
        await context.SaveChangesAsync();
    }

    public static async Task InitializeNewUserAsync(AppDbContext context, int userId)
    {
        var now = DateTime.Now;

        // Default Accounts for new user
        var primaryBank = new Account { UserId = userId, Name = "Primary Bank Account", Type = AccountType.Bank, Balance = 25000m, Icon = "🏦", Color = "#2563EB", IsDefault = true };
        var cash = new Account { UserId = userId, Name = "Cash In Hand", Type = AccountType.Cash, Balance = 3000m, Icon = "💵", Color = "#16A34A" };
        var upi = new Account { UserId = userId, Name = "UPI / Wallet", Type = AccountType.UPI, Balance = 2000m, Icon = "📱", Color = "#8B5CF6" };

        context.Accounts.AddRange(new[] { primaryBank, cash, upi });

        // Overall Monthly Budget
        context.Budgets.Add(new Budget
        {
            UserId = userId,
            CategoryId = null,
            LimitAmount = 30000m,
            Month = now.Month,
            Year = now.Year,
            AlertThresholdPercent = 85,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    public static List<Category> GetDefaultCategories()
    {
        return new List<Category>
        {
            new() { Name = "Food", Icon = "🍔", Color = "#EF4444", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 1, DefaultBudget = 6000m },
            new() { Name = "Groceries", Icon = "🛒", Color = "#10B981", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 2, DefaultBudget = 5000m },
            new() { Name = "Transport", Icon = "🚕", Color = "#F59E0B", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 3, DefaultBudget = 3000m },
            new() { Name = "Rent", Icon = "🏠", Color = "#8B5CF6", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 4, DefaultBudget = 10000m },
            new() { Name = "Bills", Icon = "💡", Color = "#06B6D4", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 5, DefaultBudget = 3000m },
            new() { Name = "Recharge", Icon = "📱", Color = "#3B82F6", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 6, DefaultBudget = 1000m },
            new() { Name = "Shopping", Icon = "🛍", Color = "#EC4899", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 7, DefaultBudget = 4000m },
            new() { Name = "Entertainment", Icon = "🎬", Color = "#6366F1", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 8, DefaultBudget = 2000m },
            new() { Name = "Health", Icon = "🏥", Color = "#14B8A6", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 9, DefaultBudget = 2000m },
            new() { Name = "Education", Icon = "🎓", Color = "#84CC16", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 10, DefaultBudget = 2000m },
            new() { Name = "Travel", Icon = "✈️", Color = "#0EA5E9", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 11, DefaultBudget = 4000m },
            new() { Name = "Work", Icon = "💼", Color = "#64748B", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 12, DefaultBudget = 1500m },
            new() { Name = "Family", Icon = "👨‍👩‍👦", Color = "#F97316", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 13, DefaultBudget = 3000m },
            new() { Name = "Investment", Icon = "💰", Color = "#22C55E", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 14, DefaultBudget = 5000m },
            new() { Name = "EMI", Icon = "🏦", Color = "#A855F7", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 15, DefaultBudget = 4500m },
            new() { Name = "Other", Icon = "📦", Color = "#94A3B8", Type = TransactionType.Expense, IsSystem = true, DisplayOrder = 16, DefaultBudget = 2000m },
            new() { Name = "Salary", Icon = "💼", Color = "#10B981", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 101 },
            new() { Name = "Freelance", Icon = "💻", Color = "#3B82F6", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 102 },
            new() { Name = "Business", Icon = "🏢", Color = "#8B5CF6", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 103 },
            new() { Name = "Bonus", Icon = "🎁", Color = "#F59E0B", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 104 },
            new() { Name = "Interest", Icon = "📈", Color = "#06B6D4", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 105 },
            new() { Name = "Other Income", Icon = "💵", Color = "#22C55E", Type = TransactionType.Income, IsSystem = true, DisplayOrder = 106 }
        };
    }
}
