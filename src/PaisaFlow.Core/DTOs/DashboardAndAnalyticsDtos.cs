using PaisaFlow.Core.Entities;

namespace PaisaFlow.Core.DTOs;

public class DashboardSummaryDto
{
    public string UserName { get; set; } = "Harsh";
    public string Greeting { get; set; } = "Good evening";
    public string MonthName { get; set; } = "October 2026";
    public decimal TotalBalance { get; set; }
    public decimal CurrentMonthIncome { get; set; }
    public decimal CurrentMonthExpenses { get; set; }
    public decimal NetSavings { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal BudgetRemaining { get; set; }
    public int BudgetUsedPercent { get; set; }
    public bool IsBudgetWarning { get; set; }
    public bool HideBalance { get; set; }

    public List<CategorySpendSummaryDto> TopCategories { get; set; } = new();
    public List<Transaction> RecentTransactions { get; set; } = new();
    public List<Account> Accounts { get; set; } = new();
}

public class CategorySpendSummaryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "📦";
    public string Color { get; set; } = "#6366F1";
    public decimal TotalAmount { get; set; }
    public decimal? BudgetAmount { get; set; }
    public int PercentageOfTotal { get; set; }
}

public class DailySpendTrendDto
{
    public int Day { get; set; }
    public string DateStr { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class AnalyticsDto
{
    public string MonthName { get; set; } = "October 2026";
    public decimal TotalSpending { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal DailyAverage { get; set; }
    public List<CategorySpendSummaryDto> CategoryBreakdown { get; set; } = new();
    public List<DailySpendTrendDto> DailyTrends { get; set; } = new();
    public List<string> SmartInsights { get; set; } = new();
}

public class SettlementItemDto
{
    public string FromMemberName { get; set; } = string.Empty;
    public string ToMemberName { get; set; } = string.Empty;
    public string? ToUpiId { get; set; }
    public decimal Amount { get; set; }
    public string UpiPaymentUrl { get; set; } = string.Empty;
}

public class GroupSettlementResultDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public decimal PerPersonShare { get; set; }
    public Dictionary<string, decimal> NetBalances { get; set; } = new();
    public List<SettlementItemDto> Settlements { get; set; } = new();
}
