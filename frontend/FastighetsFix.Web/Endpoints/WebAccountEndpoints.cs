using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastighetsFix.Web.Authentication;
using FastighetsFix.Web.Models.Authentication;
using FastighetsFix.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FastighetsFix.Web.Endpoints;

public static class WebAccountEndpoints
{
    public static IEndpointRouteBuilder MapWebAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous();

        endpoints.MapPost("/auth/logout", LogoutAsync)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        AuthenticationApiClient api,
        ServerSessionStore sessions)
    {
        if (!await ValidateFormAsync(context, antiforgery))
        {
            return Results.BadRequest(
                "Formuläret kunde inte verifieras. Ladda om sidan.");
        }

        var form = await context.Request.ReadFormAsync();

        var model = new LoginForm
        {
            Email = form["Email"].ToString().Trim(),
            Password = form["Password"].ToString()
        };

        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                validationResults,
                validateAllProperties: true))
        {
            return Results.LocalRedirect("/login?error=invalid");
        }

        try
        {
            var tokens = await api.LoginAsync(model);

            if (tokens is null)
            {
                return Results.LocalRedirect("/login?error=invalid");
            }

            var profile = await api.GetCurrentUserAsync(
                tokens.AccessToken);

            if (profile is null)
            {
                return Results.LocalRedirect("/login?error=invalid");
            }

            var session = sessions.Create(tokens, profile.Id);

            var cookiePrincipal =
                WebAuthentication.CreateCookiePrincipal(
                    session.Id);

            var properties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = session.ExpiresAt,
                AllowRefresh = false
            };

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                cookiePrincipal,
                properties);

            return Results.LocalRedirect("/home");
        }
        catch (Exception exception)
            when (exception is HttpRequestException
                  or TaskCanceledException)
        {
            return Results.LocalRedirect("/login?error=unavailable");
        }
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        ServerSessionStore sessions)
    {
        if (!await ValidateFormAsync(context, antiforgery))
        {
            return Results.BadRequest(
                "Formuläret kunde inte verifieras. Ladda om sidan.");
        }

        var sessionId = context.User.FindFirstValue(
            WebAuthentication.SessionIdClaim);

        var failed = false;

        try
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await sessions.RevokeAsync(sessionId);
            }
        }
        catch (Exception exception)
            when (exception is HttpRequestException
                  or TaskCanceledException)
        {
            failed = true;
        }
        finally
        {
            await context.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return Results.LocalRedirect(
            failed
                ? "/login?error=logout"
                : "/login");
    }

    private static async Task<bool> ValidateFormAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}