using System.Security.Claims;
using FastighetsFix.Web.Models.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FastighetsFix.Web.Authentication;

public static class WebAuthentication
{
    public const string ApiClientName = "FastighetsFixApi";

    public const string SessionIdClaim = "web_session_id";

    public static ClaimsPrincipal CreateCookiePrincipal(
        string sessionId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(SessionIdClaim, sessionId)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal CreatePrincipal(
        string sessionId,
        UserProfile profile)
    {
        var claims = new List<Claim>
        {
            new(SessionIdClaim, sessionId),
            new(ClaimTypes.NameIdentifier, profile.Id),
            new(ClaimTypes.Name, profile.Email),
            new(ClaimTypes.Email, profile.Email),
            new(ClaimTypes.GivenName, profile.FirstName),
            new(ClaimTypes.Surname, profile.LastName)
        };

        claims.AddRange(
            profile.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));
    }
}