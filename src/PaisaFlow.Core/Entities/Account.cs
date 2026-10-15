using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.Entities;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Bank;
    public decimal Balance { get; set; } = 0m;
    public string Icon { get; set; } = "🏦";
    public string Color { get; set; } = "#3B82F6";
    public bool IsDefault { get; set; } = false;
    public string? AccountNumberSuffix { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
