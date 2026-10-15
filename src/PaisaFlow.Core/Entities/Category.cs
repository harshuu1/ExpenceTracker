using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "📦";
    public string Color { get; set; } = "#6366F1";
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public decimal? DefaultBudget { get; set; }
    public bool IsSystem { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
