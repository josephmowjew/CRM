using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Core.Models;

namespace UCS_CRM.Core.Helpers;

public static class CurrentUser
{
    public static async Task<ApplicationUser?> GetCurrentUserAsync(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity is not ClaimsIdentity userIdentity || !userIdentity.IsAuthenticated)
        {
            return null;
        }

        var claimsIdentifier = userIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(claimsIdentifier))
        {
            return null;
        }

        try
        {
            return await userRepository.FindByIdAsync(claimsIdentifier);
        }
        catch
        {
            // Log the exception as needed
            return null;
        }
    }
}
