using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FastighetsFix.Web.Authentication;

public class WebCookieEvents : CookieAuthenticationEvents
{
    private readonly ServerSessionStore _sessions;
    private readonly ILogger<WebCookieEvents> _logger;

    public WebCookieEvents(
        ServerSessionStore sessions,
        ILogger<WebCookieEvents> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        // Vid logout använder vi den redan skyddade cookien
        // för att identifiera sessionen som ska avslutas.
        // Logout-endpointen verifierar formulärets antiforgery-token.
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path == "/auth/logout")
        {
            return;
        }

        var sessionId = context.Principal?.FindFirstValue(
            WebAuthentication.SessionIdClaim);

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            try
            {
                var profile = await _sessions.ValidateAsync(
                    sessionId,
                    context.HttpContext.RequestAborted);

                if (profile is not null)
                {
                    context.ReplacePrincipal(
                        WebAuthentication.CreatePrincipal(
                            sessionId,
                            profile));

                    return;
                }
            }
            catch (Exception exception)
                when (exception is HttpRequestException
                      or TaskCanceledException)
            {
                _logger.LogWarning(
                    "Web-sessionen kunde inte verifieras mot API:et.");
            }
        }

        context.RejectPrincipal();

        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}