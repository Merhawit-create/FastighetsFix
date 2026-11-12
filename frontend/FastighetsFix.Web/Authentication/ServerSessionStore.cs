using System.Security.Cryptography;
using FastighetsFix.Web.Models.Authentication;
using FastighetsFix.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FastighetsFix.Web.Authentication;

public class ServerSessionStore
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServerSessionStore> _logger;

    public ServerSessionStore(
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<ServerSessionStore> logger)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public (string Id, DateTimeOffset ExpiresAt) Create(
        TokenResponse tokens,
        string userId)
    {
        if (string.IsNullOrWhiteSpace(tokens.AccessToken)
            || string.IsNullOrWhiteSpace(tokens.RefreshToken)
            || tokens.ExpiresIn <= 0)
        {
            throw new InvalidOperationException(
                "API:et returnerade inte ett komplett tokenpar.");
        }

        var id = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(7);

        var session = new WebSession
        {
            UserId = userId,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiresAt =
                now.AddSeconds(tokens.ExpiresIn),
            ExpiresAt = expiresAt
        };

        _cache.Set(Key(id), session, expiresAt);

        return (id, expiresAt);
    }

    public async Task<UserProfile?> ValidateAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = Find(sessionId);

        if (session is null)
        {
            return null;
        }

        await session.Gate.WaitAsync(cancellationToken);

        try
        {
            if (session.Closed
                || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                Close(sessionId, session);
                return null;
            }

            using var httpClient = _httpClientFactory.CreateClient(
                WebAuthentication.ApiClientName);

            var api = new AuthenticationApiClient(httpClient);

            if (!await RefreshIfNeededAsync(session, api))
            {
                Close(sessionId, session);
                return null;
            }

            var profile = await api.GetCurrentUserAsync(
                session.AccessToken);

            if (profile is null || profile.Id != session.UserId)
            {
                Close(sessionId, session);
                return null;
            }

            return profile;
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task RevokeAsync(string sessionId)
    {
        var session = Find(sessionId);

        if (session is null)
        {
            return;
        }

        await session.Gate.WaitAsync();

        try
        {
            if (session.Closed)
            {
                return;
            }

            using var httpClient = _httpClientFactory.CreateClient(
                WebAuthentication.ApiClientName);

            var api = new AuthenticationApiClient(httpClient);

            if (await RefreshIfNeededAsync(session, api))
            {
                await api.LogoutAsync(session.AccessToken);
            }
        }
        finally
        {
            Close(sessionId, session);
            session.Gate.Release();
        }
    }

    private async Task<bool> RefreshIfNeededAsync(
        WebSession session,
        AuthenticationApiClient api)
    {
        if (session.AccessTokenExpiresAt
            > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return true;
        }

        var issuedAt = DateTimeOffset.UtcNow;

        var tokens = await api.RefreshAsync(
            session.RefreshToken);

        if (tokens is null
            || string.IsNullOrWhiteSpace(tokens.AccessToken)
            || string.IsNullOrWhiteSpace(tokens.RefreshToken)
            || tokens.ExpiresIn <= 0)
        {
            return false;
        }

        session.AccessToken = tokens.AccessToken;
        session.RefreshToken = tokens.RefreshToken;
        session.AccessTokenExpiresAt =
            issuedAt.AddSeconds(tokens.ExpiresIn);

        _logger.LogInformation(
            "API-token förnyades för en Web-session.");

        return true;
    }

    private WebSession? Find(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return _cache.TryGetValue<WebSession>(
            Key(sessionId),
            out var session)
            ? session
            : null;
    }

    private void Close(
        string sessionId,
        WebSession session)
    {
        session.Closed = true;
        _cache.Remove(Key(sessionId));
    }

    private static string Key(string sessionId)
    {
        return $"web-session:{sessionId}";
    }

    private sealed class WebSession
    {
        public string UserId { get; init; } = "";

        public string AccessToken { get; set; } = "";

        public string RefreshToken { get; set; } = "";

        public DateTimeOffset AccessTokenExpiresAt { get; set; }

        public DateTimeOffset ExpiresAt { get; init; }

        public bool Closed { get; set; }

        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}