using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.DTOs;
using PaisaFlow.Core.Entities;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Infrastructure.Services;

public class SplitExpenseService : ISplitExpenseService
{
    private readonly AppDbContext _context;

    public SplitExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SplitGroup>> GetUserGroupsAsync(int userId)
    {
        return await _context.SplitGroups
            .Include(g => g.Members)
            .Include(g => g.Expenses)
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<SplitGroup?> GetGroupDetailsAsync(int userId, int groupId)
    {
        return await _context.SplitGroups
            .Include(g => g.Members)
            .Include(g => g.Expenses)
                .ThenInclude(e => e.PaidByMember)
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Participants)
                    .ThenInclude(p => p.SplitMember)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);
    }

    public async Task<SplitGroup> CreateGroupAsync(int userId, string name, List<string> memberNames, string? upiId = null)
    {
        var group = new SplitGroup
        {
            UserId = userId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        _context.SplitGroups.Add(group);
        await contextSaveAsync();

        // Add creator
        var creator = await _context.Users.FindAsync(userId);
        var creatorMember = new SplitMember
        {
            SplitGroupId = group.Id,
            Name = creator?.FullName ?? "Me",
            UpiId = upiId ?? "harsh@okaxis",
            IsCurrentUser = true
        };
        _context.SplitMembers.Add(creatorMember);

        // Add other members
        foreach (var m in memberNames.Where(s => !string.IsNullOrWhiteSpace(s) && !s.Equals(creatorMember.Name, StringComparison.OrdinalIgnoreCase)))
        {
            _context.SplitMembers.Add(new SplitMember
            {
                SplitGroupId = group.Id,
                Name = m.Trim(),
                UpiId = $"{m.Trim().ToLower()}@upi",
                IsCurrentUser = false
            });
        }

        await contextSaveAsync();
        return group;
    }

    public async Task<SplitExpense> AddExpenseToGroupAsync(int userId, int groupId, string title, decimal amount, int paidByMemberId, List<int>? participantIds = null)
    {
        var group = await _context.SplitGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.UserId == userId);

        if (group == null) throw new InvalidOperationException("Group not found");

        var expense = new SplitExpense
        {
            SplitGroupId = groupId,
            Title = title,
            Amount = amount,
            PaidByMemberId = paidByMemberId,
            Date = DateTime.Now
        };
        _context.SplitExpenses.Add(expense);
        await contextSaveAsync();

        var members = group.Members.ToList();
        var selectedMembers = (participantIds != null && participantIds.Any())
            ? members.Where(m => participantIds.Contains(m.Id)).ToList()
            : members;

        if (!selectedMembers.Any()) selectedMembers = members;

        var share = Math.Round(amount / selectedMembers.Count, 2);
        foreach (var m in selectedMembers)
        {
            _context.SplitParticipants.Add(new SplitParticipant
            {
                SplitExpenseId = expense.Id,
                SplitMemberId = m.Id,
                ShareAmount = share
            });
        }

        await contextSaveAsync();
        return expense;
    }

    public async Task<GroupSettlementResultDto> CalculateSettlementsAsync(int userId, int groupId)
    {
        var group = await GetGroupDetailsAsync(userId, groupId);
        if (group == null) return new GroupSettlementResultDto();

        var totalExpenses = group.Expenses.Sum(e => e.Amount);
        var netBalances = new Dictionary<int, decimal>(); // MemberId -> Net balance (+ means owed to them, - means they owe)

        foreach (var m in group.Members)
        {
            netBalances[m.Id] = 0m;
        }

        foreach (var exp in group.Expenses)
        {
            if (netBalances.ContainsKey(exp.PaidByMemberId))
            {
                netBalances[exp.PaidByMemberId] += exp.Amount;
            }

            foreach (var part in exp.Participants)
            {
                if (netBalances.ContainsKey(part.SplitMemberId))
                {
                    netBalances[part.SplitMemberId] -= part.ShareAmount;
                }
            }
        }

        // Min cash flow settlement algorithm
        var creditors = netBalances.Where(kvp => kvp.Value > 0.01m)
            .Select(kvp => new { Id = kvp.Key, Amount = kvp.Value })
            .OrderByDescending(c => c.Amount)
            .ToList();

        var debtors = netBalances.Where(kvp => kvp.Value < -0.01m)
            .Select(kvp => new { Id = kvp.Key, Amount = -kvp.Value })
            .OrderByDescending(d => d.Amount)
            .ToList();

        var memberDict = group.Members.ToDictionary(m => m.Id);
        var settlements = new List<SettlementItemDto>();

        var cIdx = 0;
        var dIdx = 0;
        var curCreditors = creditors.Select(c => c.Amount).ToList();
        var curDebtors = debtors.Select(d => d.Amount).ToList();

        while (cIdx < creditors.Count && dIdx < debtors.Count)
        {
            var credId = creditors[cIdx].Id;
            var debtId = debtors[dIdx].Id;

            var settleAmount = Math.Min(curCreditors[cIdx], curDebtors[dIdx]);
            if (settleAmount > 0.01m)
            {
                var credMember = memberDict[credId];
                var debtMember = memberDict[debtId];

                var upi = credMember.UpiId ?? "harsh@okaxis";
                var upiUrl = $"upi://pay?pa={Uri.EscapeDataString(upi)}&pn={Uri.EscapeDataString(credMember.Name)}&am={settleAmount:F2}&cu=INR&tn={Uri.EscapeDataString($"{group.Name} Settlement")}";

                settlements.Add(new SettlementItemDto
                {
                    FromMemberName = debtMember.Name,
                    ToMemberName = credMember.Name,
                    ToUpiId = credMember.UpiId,
                    Amount = settleAmount,
                    UpiPaymentUrl = upiUrl
                });
            }

            curCreditors[cIdx] -= settleAmount;
            curDebtors[dIdx] -= settleAmount;

            if (curCreditors[cIdx] <= 0.01m) cIdx++;
            if (curDebtors[dIdx] <= 0.01m) dIdx++;
        }

        var netDisplay = netBalances.ToDictionary(
            kvp => memberDict.ContainsKey(kvp.Key) ? memberDict[kvp.Key].Name : kvp.Key.ToString(),
            kvp => kvp.Value
        );

        return new GroupSettlementResultDto
        {
            GroupId = group.Id,
            GroupName = group.Name,
            TotalExpenses = totalExpenses,
            PerPersonShare = group.Members.Count > 0 ? Math.Round(totalExpenses / group.Members.Count, 2) : 0,
            NetBalances = netDisplay,
            Settlements = settlements
        };
    }

    private Task<int> contextSaveAsync() => _context.SaveChangesAsync();
}
