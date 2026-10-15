using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.Entities;

public class RecurringTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserAccount? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public string PaymentMethod { get; set; } = "UPI";
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    public int DayOfMonth { get; set; } = 1; // e.g. 1st, 7th, 15th
    public DateTime NextDueDate { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoProcess { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
