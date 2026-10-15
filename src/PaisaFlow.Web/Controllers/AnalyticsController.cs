using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Interfaces;
using PaisaFlow.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using PaisaFlow.Web.Helpers;

namespace PaisaFlow.Web.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly AppDbContext _context;

    public AnalyticsController(IAnalyticsService analyticsService, AppDbContext context)
    {
        _analyticsService = analyticsService;
        _context = context;
    }

    private int GetCurrentUserId() => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var userId = GetCurrentUserId();
        var analytics = await _analyticsService.GetAnalyticsAsync(userId, month, year);
        return View(analytics);
    }
}
