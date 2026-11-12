using System.Security.Claims;
using FastighetsFix.Application.Authentication;
using FastighetsFix.Application.Authentication.Dtos;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FastighetsFix.Infrastructure.Authentication;

public class TokenRefreshService : ITokenRefreshService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOptionsMonitor<BearerTokenOptions> _tokenOptions;

    public TokenRefreshService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> tokenOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenOptions = tokenOptions;
    }

    public async Task<LoginResult> RefreshAsync(
        RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new LoginResult();
        }

        var options = _tokenOptions.Get(
            IdentityConstants.BearerScheme);

        var ticket = options.RefreshTokenProtector.Unprotect(
            request.RefreshToken);

        if (ticket is null
            || ticket.Properties.ExpiresUtc is not { } expiresAt
            || expiresAt <= DateTimeOffset.UtcNow)
        {
            return new LoginResult();
        }

        var userId = ticket.Principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var tokenStamp = ticket.Principal.FindFirstValue(
            AuthenticationClaimTypes.SecurityStamp);

        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(tokenStamp))
        {
            return new LoginResult();
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return new LoginResult();
        }

        var currentStamp =
            await _userManager.GetSecurityStampAsync(user);

        if (!string.Equals(
                tokenStamp,
                currentStamp,
                StringComparison.Ordinal))
        {
            return new LoginResult();
        }

        if (!await _signInManager.CanSignInAsync(user)
            || await _userManager.IsLockedOutAsync(user)
            || await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return new LoginResult();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new LoginResult
        {
            SecurityStamp = currentStamp,

            User = new CurrentUserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                Roles = roles.ToArray()
            }
        };
    }
}