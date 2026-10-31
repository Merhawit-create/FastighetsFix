using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastighetsFix.Api.Authorization;
using FastighetsFix.Api.Endpoints;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace FastighetsFix.IntegrationTests;

public class AdminAuthorizationTests
{
    [Theory]
    [InlineData(null, 401)]
    [InlineData(AppRoles.Tenant, 403)]
    [InlineData(AppRoles.Technician, 403)]
    [InlineData(AppRoles.Admin, 200)]
    public async Task Admin_endpoint_requires_admin_role(
        string? role,
        int expectedStatusCode)
    {
        // Arrange: skapa en testserver med appens riktiga
        // admin-endpoint och behörighetspolicy.
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services
                    .AddAuthentication("Test")
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthenticationHandler>(
                            "Test",
                            _ => { });

                services.AddAppAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapAdminEndpoints();
                });
            });

        using var server = new TestServer(builder);
        using var client = server.CreateClient();

        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(
                "X-Test-Role",
                role);
        }

        // Act: gör ett HTTP-anrop.
        using var response = await client.GetAsync(
            "/api/admin/access-check");

        // Assert: kontrollera resultatet.
        Assert.Equal(
            (HttpStatusCode)expectedStatusCode,
            response.StatusCode);
    }
}

// Denna autentisering används ENDAST i testprojektet.
public class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                "X-Test-Role",
                out var role))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "test-user"),

            new Claim(
                ClaimTypes.Role,
                role.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}