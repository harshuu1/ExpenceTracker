using PaisaFlow.Core.Enums;

namespace PaisaFlow.Core.DTOs;

public class QuickAddRequest
{
    public string Input { get; set; } = string.Empty;
}

public class QuickAddResult
{
    public bool Success { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string CategoryName { get; set; } = "Other";
    public string CategoryIcon { get; set; } = "📦";
    public int? CategoryId { get; set; }
    public string PaymentMethod { get; set; } = "UPI";
    public int? AccountId { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Message { get; set; } = string.Empty;
}

public class TransactionCreateDto
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public int? CategoryId { get; set; }
    public int? AccountId { get; set; }
    public string PaymentMethod { get; set; } = "UPI";
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public string? MerchantOrPayee { get; set; }
}

public class TransactionFilterDto
{
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public int? AccountId { get; set; }
    public string? PaymentMethod { get; set; }
    public TransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
