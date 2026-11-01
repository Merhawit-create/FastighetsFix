using System.Security.Claims;
using FastighetsFix.Web.Models.Authentication;
using FastighetsFix.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace FastighetsFix.Web.Authentication;

public class ApiAuthenticationStateProvider
    : AuthenticationStateProvider, IDisposable
{
    private readonly AuthenticationApiClient _apiClient;

    private ClaimsPrincipal _user = CreateAnonymousUser();
    private string? _accessToken;
    private DateTimeOffset _expiresAt;
    private CancellationTokenSource? _expiryCancellation;

    public ApiAuthenticationStateProvider(
        AuthenticationApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        if (_accessToken is not null
            && DateTimeOffset.UtcNow >= _expiresAt)
        {
            ClearSession();
        }

        return Task.FromResult(
            new AuthenticationState(_user));
    }

    public async Task<bool> LoginAsync(LoginForm model)
    {
        Logout();

        var token = await _apiClient.LoginAsync(model);

        if (token is null
            || string.IsNullOrWhiteSpace(token.AccessToken)
            || token.ExpiresIn <= 0)
        {
            return false;
        }

        var expiresAt = DateTimeOffset.UtcNow
            .AddSeconds(token.ExpiresIn);

        // Hämta profil med token innan vi betraktar
        // användaren som inloggad i Blazor.
        var profile = await _apiClient.GetCurrentUserAsync(
            token.AccessToken);

        if (profile is null || expiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.Id),
            new(ClaimTypes.Name, profile.Email),
            new(ClaimTypes.Email, profile.Email),
            new(ClaimTypes.GivenName, profile.FirstName),
            new(ClaimTypes.Surname, profile.LastName)
        };

        claims.AddRange(
            profile.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        _user = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "ApiBearer"));

        _accessToken = token.AccessToken;
        _expiresAt = expiresAt;

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_user)));

        _expiryCancellation = new CancellationTokenSource();

        _ = ExpireSessionAsync(
            expiresAt,
            _expiryCancellation.Token);

        return true;
    }

    public string? GetAccessToken()
    {
        if (_accessToken is not null
            && DateTimeOffset.UtcNow >= _expiresAt)
        {
            Logout();
        }

        return _accessToken;
    }

    public void Logout()
    {
        ClearSession();

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_user)));
    }

    private void ClearSession()
    {
        _expiryCancellation?.Cancel();
        _expiryCancellation?.Dispose();
        _expiryCancellation = null;

        _accessToken = null;
        _expiresAt = default;
        _user = CreateAnonymousUser();
    }

    private async Task ExpireSessionAsync(
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var delay = expiresAt - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                Logout();
            }
        }
        catch (OperationCanceledException)
        {
            // Sessionen avslutades eller ersattes.
        }
    }

    private static ClaimsPrincipal CreateAnonymousUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    public void Dispose()
    {
        ClearSession();
    }
}