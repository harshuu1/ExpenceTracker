using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class SplitController : Controller
{
    private readonly ISplitExpenseService _splitService;
    private readonly AppDbContext _context;

    public SplitController(ISplitExpenseService splitService, AppDbContext context)
    {
        _splitService = splitService;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index(int? groupId)
    {
        var userId = GetCurrentUserId();
        var groups = await _splitService.GetUserGroupsAsync(userId);

        var activeGroupId = groupId ?? groups.FirstOrDefault()?.Id;
        if (!activeGroupId.HasValue)
        {
            return View(null);
        }

        var groupDetails = await _splitService.GetGroupDetailsAsync(userId, activeGroupId.Value);
        var settlement = await _splitService.CalculateSettlementsAsync(userId, activeGroupId.Value);

        ViewBag.Groups = groups;
        ViewBag.ActiveGroup = groupDetails;
        ViewBag.Settlement = settlement;

        return View(groupDetails);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(string name, string members, string? upiId)
    {
        var userId = GetCurrentUserId();
        var memberList = members.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var group = await _splitService.CreateGroupAsync(userId, name, memberList, upiId);
        TempData["Success"] = $"Group '{name}' created!";
        return RedirectToAction(nameof(Index), new { groupId = group.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExpense(int groupId, string title, decimal amount, int paidByMemberId, List<int>? participantIds)
    {
        var userId = GetCurrentUserId();
        await _splitService.AddExpenseToGroupAsync(userId, groupId, title, amount, paidByMemberId, participantIds);
        TempData["Success"] = $"Expense '{title}' added to group!";
        return RedirectToAction(nameof(Index), new { groupId = groupId });
    }
}
