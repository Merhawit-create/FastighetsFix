using FastighetsFix.Application.Authentication;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FastighetsFix.Infrastructure.Authentication;

public class AccessTokenService : IAccessTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessTokenService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> IsValidAsync(string userId, string securityStamp)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(securityStamp))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        var currentStamp = await _userManager.GetSecurityStampAsync(user);
        return string.Equals(currentStamp, securityStamp, StringComparison.Ordinal);

    }

    public async Task<bool> RevokeAllAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return false;
        }

        var result = await _userManager.UpdateSecurityStampAsync(user);

        return result.Succeeded;
    }
}