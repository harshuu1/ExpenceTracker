using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Entities;

namespace PaisaFlow.Core.Interfaces;

public interface ITransactionService
{
    Task<Transaction> AddTransactionAsync(int userId, TransactionCreateDto dto);
    Task<Transaction?> GetTransactionByIdAsync(int userId, int transactionId);
    Task<bool> UpdateTransactionAsync(int userId, int transactionId, TransactionCreateDto dto);
    Task<bool> DeleteTransactionAsync(int userId, int transactionId);
    Task<List<Transaction>> GetFilteredTransactionsAsync(int userId, TransactionFilterDto filter);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int? month = null, int? year = null);
}

public interface IQuickAddParser
{
    Task<QuickAddResult> ParseInputAsync(int userId, string input);
}

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAnalyticsAsync(int userId, int? month = null, int? year = null);
}

public interface IBudgetService
{
    Task<List<Budget>> GetBudgetsAsync(int userId, int? month = null, int? year = null);
    Task<Budget> SaveBudgetAsync(int userId, int? categoryId, decimal limitAmount, int month, int year);
    Task<bool> DeleteBudgetAsync(int userId, int budgetId);
}

public interface ISplitExpenseService
{
    Task<List<SplitGroup>> GetUserGroupsAsync(int userId);
    Task<SplitGroup?> GetGroupDetailsAsync(int userId, int groupId);
    Task<SplitGroup> CreateGroupAsync(int userId, string name, List<string> memberNames, string? upiId = null);
    Task<SplitExpense> AddExpenseToGroupAsync(int userId, int groupId, string title, decimal amount, int paidByMemberId, List<int>? participantIds = null);
    Task<GroupSettlementResultDto> CalculateSettlementsAsync(int userId, int groupId);
}

public interface IExportService
{
    Task<byte[]> ExportCsvAsync(int userId, int? month = null, int? year = null);
    Task<byte[]> ExportExcelTsvAsync(int userId, int? month = null, int? year = null);
    Task<string> ExportJsonBackupAsync(int userId);
    Task<bool> ImportJsonBackupAsync(int userId, string jsonContent);
}
