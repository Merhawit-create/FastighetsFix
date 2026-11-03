using System.Security.Claims;
using FastighetsFix.Application.Authentication;

namespace FastighetsFix.Api.Middleware;

public class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRevocationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAccessTokenService accessTokenService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var securityStamp = context.User.FindFirstValue(
                AuthenticationClaimTypes.SecurityStamp);

            var isValid =
                !string.IsNullOrWhiteSpace(userId)
                && !string.IsNullOrWhiteSpace(securityStamp)
                && await accessTokenService.IsValidAsync(
                    userId,
                    securityStamp);

            if (!isValid)
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.Headers["WWW-Authenticate"] =
                    "Bearer";

                return;
            }
        }

        await _next(context);
    }
}