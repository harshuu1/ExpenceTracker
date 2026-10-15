using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserAccount? User { get; set; }

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public string PaymentMethod { get; set; } = "UPI"; // UPI, Cash, Debit Card, Credit Card, Bank Transfer, Wallet
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public string? MerchantOrPayee { get; set; }
    public string? Tags { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
