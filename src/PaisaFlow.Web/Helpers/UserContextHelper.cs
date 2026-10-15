using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PaisaFlow.Core.Entities;
using PaisaFlow.Infrastructure.Data;

namespace PaisaFlow.Web.Helpers;

public static class UserContextHelper
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id) && id > 0)
        {
            return id;
        }
        return 1; // Fallback for unauthenticated development if needed
    }

    public static async Task<UserAccount?> GetActiveUserAsync(this ClaimsPrincipal user, AppDbContext context)
    {
        var id = user.GetUserId();
        return await context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public static string GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "User";
    }

    public static string GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? "";
    }
}
