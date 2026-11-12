using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace FastighetsFix.Web.Authentication;

public class SessionAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly ServerSessionStore _sessions;

    public SessionAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        ServerSessionStore sessions)
        : base(loggerFactory)
    {
        _sessions = sessions;
    }

    protected override TimeSpan RevalidationInterval =>
        TimeSpan.FromMinutes(1);

    protected override async Task<bool>
        ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState,
            CancellationToken cancellationToken)
    {
        var sessionId =
            authenticationState.User.FindFirstValue(
                WebAuthentication.SessionIdClaim);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        try
        {
            var profile = await _sessions.ValidateAsync(
                sessionId,
                cancellationToken);

            return profile is not null;
        }
        catch (Exception exception)
            when (exception is HttpRequestException
                      or TaskCanceledException)
        {
            return false;
        }
    }
}