using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.Entities;

public class UserAccount
{
    public int Id { get; set; }
    public string FullName { get; set; } = "Harsh";
    public string Email { get; set; } = "harsh@example.com";
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Currency { get; set; } = "₹";
    public decimal MonthlyBudgetLimit { get; set; } = 42000m;
    public bool HideBalance { get; set; } = false;
    public string? PinCode { get; set; } = "1234";
    public bool IsBiometricEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Category> CustomCategories { get; set; } = new List<Category>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
    public ICollection<SplitGroup> SplitGroups { get; set; } = new List<SplitGroup>();
}
