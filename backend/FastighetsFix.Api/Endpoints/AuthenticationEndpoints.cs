using System.Security.Claims;
using FastighetsFix.Application.Authentication;
using FastighetsFix.Application.Authentication.Dtos;
using Microsoft.AspNetCore.Identity;

namespace FastighetsFix.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {    
     
        endpoints.MapPost("/api/auth/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("Register")
            .WithTags("Authentication");

        endpoints.MapPost(
                "/api/auth/login",
                LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithTags("Authentication");

        endpoints.MapGet(
                "/api/auth/me",
                GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithTags("Authentication");
        
        endpoints.MapPost(
                "/api/auth/logout",
                LogoutAsync)
            .RequireAuthorization()
            .WithName("Logout")
            .WithTags("Authentication");
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request,
        IRegistrationService registrationService)
    {
        var result = await registrationService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(result.Errors);
        }

        return Results.StatusCode(StatusCodes.Status201Created);
    }
    
    
    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ILoginService loginService)
    {
        var result = await loginService.LoginAsync(request);

        if (!result.Succeeded || result.User is null)
        {
            return Results.Unauthorized();
        }

        var user = result.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            
            new(
                AuthenticationClaimTypes.SecurityStamp,
                result.SecurityStamp)
        };

        claims.AddRange(
            user.Roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(
            claims,
            IdentityConstants.BearerScheme);

        var principal = new ClaimsPrincipal(identity);

        return Results.SignIn(
            principal,
            authenticationScheme: IdentityConstants.BearerScheme);
    }

    private static IResult GetCurrentUser(
        ClaimsPrincipal principal)
    {
        var user = new CurrentUserResponse
        {
            Id = principal.FindFirstValue(
                ClaimTypes.NameIdentifier) ?? "",

            FirstName = principal.FindFirstValue(
                ClaimTypes.GivenName) ?? "",

            LastName = principal.FindFirstValue(
                ClaimTypes.Surname) ?? "",

            Email = principal.FindFirstValue(
                ClaimTypes.Email) ?? "",

            Roles = principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToArray()
        };

        return Results.Ok(user);
    }  
    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal principal,
        IAccessTokenService accessTokenService)
    {
        var userId = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var succeeded =
            await accessTokenService.RevokeAllAsync(userId);

        if (!succeeded)
        {
            return Results.Problem(
                title: "Utloggningen kunde inte slutföras.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.NoContent();
    }
}